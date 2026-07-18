namespace NeuroNotes.Platform.Public.Behaviors;

/// <summary>Where a behavior came from — shown via <c>GET /admin/behaviors</c>.</summary>
public sealed record BehaviorDescriptor(string Key, string DisplayName, string Source);

/// <summary>
/// The set of bot behaviors known to the running platform: built-ins registered at startup, plus any
/// operator-supplied behavior extensions loaded later (FR-005). Registration logic (contract-version
/// check, key-collision rejection) is uniform regardless of source, so a bad extension can never
/// corrupt or replace an existing behavior (FR-006).
/// </summary>
public interface IBehaviorCatalog
{
    bool TryGet(string key, out IBotBehavior? behavior);

    IReadOnlyList<BehaviorDescriptor> List();

    /// <summary>
    /// Registers a behavior discovered from <paramref name="source"/> (e.g. "built-in" or
    /// "extension:Reminders.dll"). Fails if its <see cref="IBotBehavior.ContractVersion"/> major version
    /// differs from <see cref="BehaviorContractVersion.Current"/>, or if its <see cref="IBotBehavior.Key"/>
    /// collides with an already-registered behavior.
    /// </summary>
    Result Register(IBotBehavior behavior, string source);
}