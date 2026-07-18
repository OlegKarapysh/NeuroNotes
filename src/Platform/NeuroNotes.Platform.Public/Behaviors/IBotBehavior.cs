namespace NeuroNotes.Platform.Public.Behaviors;

/// <summary>
/// The plugin contract for bot behaviors (FR-003–FR-007). Built-in behaviors and operator-supplied
/// behavior extensions implement the same interface, so the platform treats them uniformly.
/// </summary>
public interface IBotBehavior
{
    /// <summary>Stable unique identifier stored on a <c>BotRegistration</c> (e.g. "note-capture"). Kebab-case.</summary>
    string Key { get; }

    /// <summary>Human-friendly name shown in <c>GET /admin/behaviors</c>.</summary>
    string DisplayName { get; }

    /// <summary>SDK contract version this behavior was built against — see <see cref="BehaviorContractVersion"/>.</summary>
    string ContractVersion { get; }

    /// <summary>
    /// Handle one update for one bot. Should not throw for expected failures; any throw is caught and
    /// contained by the platform (FR-006) but marks the bot's health (FR-022).
    /// </summary>
    Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken);
}