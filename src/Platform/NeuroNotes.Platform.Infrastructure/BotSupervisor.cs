namespace NeuroNotes.Platform.Infrastructure;

/// <summary>
/// Starts and stops the per-bot receiver (long-polling in Development, a webhook otherwise) that feeds
/// <see cref="BotUpdate"/>s onto the bus. Registering/removing/disabling one bot only ever touches that
/// bot's entry here — every other running bot is untouched (FR-009).
/// </summary>
public sealed class BotSupervisor(
    PollingBotReceiver pollingReceiver,
    WebhookSecretProvider webhookSecretProvider,
    IBotClientRegistry botClientRegistry,
    IHostEnvironment environment,
    IOptions<PlatformOptions> platformOptions,
    ILogger<BotSupervisor> logger) : IBotLifecycle
{
    private readonly ConcurrentDictionary<long, RunningBot> _runningBots = new();

    /// <summary>Starts (or restarts, e.g. after a token rotation) the receiver for <paramref name="botId"/>.</summary>
    public async Task StartAsync(long botId, string token, CancellationToken cancellationToken)
    {
        botClientRegistry.Set(botId, token);
        var client = botClientRegistry.Get(botId);

        StopReceiveLoop(botId);

        if (environment.IsDevelopment())
        {
            var cts = new CancellationTokenSource();
            var loopToken = cts.Token;
            var receiveLoop = Task.Run(() => RunPollingLoop(botId, client, loopToken), CancellationToken.None);
            _runningBots[botId] = new RunningBot(cts, receiveLoop, IsWebhook: false);
        }
        else
        {
            var baseUrl = platformOptions.Value.WebhookBaseUrl
                ?? throw new InvalidOperationException("Platform:WebhookBaseUrl is required outside Development.");
            var secret = webhookSecretProvider.GetSecret(botId);

            await client.SetWebhook(
                url: $"{baseUrl.TrimEnd('/')}/{botId}",
                secretToken: secret,
                cancellationToken: cancellationToken);

            _runningBots[botId] = new RunningBot(Cts: null, ReceiveLoop: null, IsWebhook: true);
        }

        logger.LogInformation("Bot {BotId} started ({Mode})", botId, environment.IsDevelopment() ? "polling" : "webhook");
    }

    /// <summary>Stops serving <paramref name="botId"/> without forgetting its client (used by disable).</summary>
    public async Task StopAsync(long botId, CancellationToken cancellationToken)
    {
        if (_runningBots.TryGetValue(botId, out var running) && running.IsWebhook
            && botClientRegistry.TryGet(botId, out var client) && client is not null)
        {
            try
            {
                await client.DeleteWebhook(cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to delete webhook for bot {BotId}.", botId);
            }
        }

        StopReceiveLoop(botId);
        logger.LogInformation("Bot {BotId} stopped", botId);
    }

    /// <summary>Stops the bot and forgets its client entirely (used by remove).</summary>
    public async Task RemoveAsync(long botId, CancellationToken cancellationToken)
    {
        await StopAsync(botId, cancellationToken);
        botClientRegistry.Remove(botId);
    }

    private async Task RunPollingLoop(long botId, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        try
        {
            await client.DeleteWebhook(cancellationToken: cancellationToken);
            await pollingReceiver.RunAsync(botId, client, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on stop/rotate/disable/remove.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Polling loop for bot {BotId} terminated unexpectedly.", botId);
        }
    }

    private void StopReceiveLoop(long botId)
    {
        if (_runningBots.TryRemove(botId, out var running))
        {
            running.Cts?.Cancel();
            running.Cts?.Dispose();
        }
    }

    private sealed record RunningBot(CancellationTokenSource? Cts, Task? ReceiveLoop, bool IsWebhook);
}