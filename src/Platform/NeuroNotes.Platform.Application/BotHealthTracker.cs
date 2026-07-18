namespace NeuroNotes.Platform.Application;

/// <summary>
/// Tracks per-bot update-handling health. After repeated consecutive failures a bot is marked
/// <see cref="BotStatus.Failing"/> (FR-022) while its receiver keeps running at normal cadence — no
/// backoff, no auto-disable; the operator decides whether to disable/rotate/remove. A later success
/// clears the flag back to <see cref="BotStatus.Active"/>. Never touches a bot the operator has
/// explicitly disabled.
/// </summary>
public sealed class BotHealthTracker(IBotRegistry botRegistry, ILogger<BotHealthTracker> logger)
{
    public const int FailureThreshold = 3;

    private readonly ConcurrentDictionary<long, int> _consecutiveFailures = new();

    public async Task RecordFailure(long botId, CancellationToken cancellationToken = default)
    {
        var failures = _consecutiveFailures.AddOrUpdate(botId, 1, (_, count) => count + 1);
        if (failures < FailureThreshold)
        {
            return;
        }

        var registration = await botRegistry.GetAsync(botId, cancellationToken);
        if (registration is null || registration.Status == BotStatus.Disabled)
        {
            return;
        }

        if (registration.Status != BotStatus.Failing)
        {
            await botRegistry.UpdateStatusAsync(botId, BotStatus.Failing, cancellationToken);
            logger.LogWarning("Bot {BotId} marked Failing after {Failures} consecutive errors.", botId, failures);
        }
    }

    public async Task RecordSuccess(long botId, CancellationToken cancellationToken = default)
    {
        var hadFailures = _consecutiveFailures.TryRemove(botId, out var previousCount) && previousCount > 0;
        if (!hadFailures)
        {
            return;
        }

        var registration = await botRegistry.GetAsync(botId, cancellationToken);
        if (registration is { Status: BotStatus.Failing })
        {
            await botRegistry.UpdateStatusAsync(botId, BotStatus.Active, cancellationToken);
            logger.LogInformation("Bot {BotId} recovered; status reset to Active.", botId);
        }
    }
}