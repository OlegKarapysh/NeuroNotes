namespace NeuroNotes.Platform.Infrastructure.Receivers;

/// <summary>
/// Runs one bot's long-polling receive loop (Development). A fresh DI scope is created per received update
/// or error (mirroring the pre-platform <c>TelegramPollingService</c>), so publishing/health-tracking never
/// captures a scoped service into this singleton.
/// </summary>
public sealed class PollingBotReceiver(IServiceScopeFactory serviceScopeFactory, ILogger<PollingBotReceiver> logger)
{
    public Task RunAsync(long botId, ITelegramBotClient client, CancellationToken cancellationToken) =>
        client.ReceiveAsync(
            updateHandler: (_, update, ct) => HandleUpdate(botId, update, ct),
            errorHandler: (_, exception, ct) => HandleError(botId, exception, ct),
            receiverOptions: new ReceiverOptions
            {
                AllowedUpdates = [],
                DropPendingUpdates = true
            },
            cancellationToken: cancellationToken);

    private async Task HandleUpdate(long botId, Update update, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(new BotUpdate(botId, update), cancellationToken);

        // A successful poll means the token/connection are healthy — clears any accumulated polling errors.
        var healthTracker = scope.ServiceProvider.GetRequiredService<BotHealthTracker>();
        await healthTracker.RecordSuccess(botId, cancellationToken);
    }

    private async Task HandleError(long botId, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Polling error for bot {BotId}", botId);

        using var scope = serviceScopeFactory.CreateScope();
        var healthTracker = scope.ServiceProvider.GetRequiredService<BotHealthTracker>();
        await healthTracker.RecordFailure(botId, cancellationToken);
    }
}