namespace NeuroNotes.Platform.Application;

/// <summary>
/// Resolves the bot behind a <see cref="BotUpdate"/> and dispatches to its assigned <see cref="IBotBehavior"/>.
/// A fault in one bot's update — a bad behavior, a downstream error — is contained here (FR-006, FR-021) and
/// never stops another bot's updates from being processed.
/// </summary>
public sealed class BotUpdateRouter(
    IBotRegistry botRegistry,
    IBehaviorCatalog behaviorCatalog,
    ITelegramBotClient telegramBotClient,
    IServiceProvider serviceProvider,
    BotHealthTracker healthTracker,
    ILogger<BotUpdateRouter> logger) : IConsumer<BotUpdate>
{
    public Task Consume(ConsumeContext<BotUpdate> context) =>
        Route(context.Message.BotId, context.Message.Update, context.CancellationToken);

    /// <summary>The routing logic itself, factored out of <see cref="Consume"/> so it is testable without a MassTransit consume context.</summary>
    public async Task Route(long botId, Update update, CancellationToken cancellationToken)
    {
        // Tags the current trace with the owning bot so per-bot latency/failures stay measurable as the
        // fleet grows (constitution: "Observability MUST be preserved and extended with new features").
        System.Diagnostics.Activity.Current?.SetTag("bot.id", botId);

        var registration = await botRegistry.GetAsync(botId, cancellationToken);
        if (registration is null)
        {
            logger.LogWarning("Received an update for unknown bot {BotId}; dropping it.", botId);
            return;
        }

        if (!behaviorCatalog.TryGet(registration.BehaviorKey, out var behavior) || behavior is null)
        {
            logger.LogError(
                "Bot {BotId} is assigned unknown behavior {BehaviorKey}; dropping its update.",
                botId, registration.BehaviorKey);
            return;
        }

        var updateContext = new BotUpdateContext(botId, update, telegramBotClient, serviceProvider);

        try
        {
            await behavior.HandleUpdateAsync(updateContext, cancellationToken);
            await healthTracker.RecordSuccess(botId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Contain the fault to this bot/update — never let it propagate and stall other bots (FR-021).
            logger.LogError(exception, "Behavior {BehaviorKey} failed handling an update for bot {BotId}.", registration.BehaviorKey, botId);
            await healthTracker.RecordFailure(botId, cancellationToken);
        }
    }

    private sealed class BotUpdateContext(long botId, Update update, ITelegramBotClient client, IServiceProvider services)
        : IBotUpdateContext
    {
        public long BotId { get; } = botId;
        public Update Update { get; } = update;
        public ITelegramBotClient Client { get; } = client;
        public IServiceProvider Services { get; } = services;
    }
}