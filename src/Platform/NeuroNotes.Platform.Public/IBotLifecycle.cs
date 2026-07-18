namespace NeuroNotes.Platform.Public;

/// <summary>
/// Starts/stops a bot's receiver (long-polling or webhook). Implemented by the Infrastructure-level
/// <c>BotSupervisor</c>; depended on from Application (<c>BotRegistrationService</c>) through this
/// abstraction so Application never references Infrastructure directly.
/// </summary>
public interface IBotLifecycle
{
    Task StartAsync(long botId, string token, CancellationToken cancellationToken = default);
    Task StopAsync(long botId, CancellationToken cancellationToken = default);
    Task RemoveAsync(long botId, CancellationToken cancellationToken = default);
}