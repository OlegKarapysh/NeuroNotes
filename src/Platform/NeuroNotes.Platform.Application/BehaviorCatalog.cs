namespace NeuroNotes.Platform.Application;

/// <summary>
/// The set of bot behaviors known to the running platform. Registration is uniform for built-in behaviors
/// (registered at startup) and operator-supplied extensions (registered after being loaded by
/// <c>ExtensionAssemblyLoader</c>) — pure in-memory logic, deliberately free of any disk/reflection concern
/// so it stays unit-testable against a fake behavior.
/// </summary>
public sealed class BehaviorCatalog : IBehaviorCatalog
{
    private readonly ConcurrentDictionary<string, (IBotBehavior Behavior, string Source)> _behaviorsByKey = new();

    public bool TryGet(string key, out IBotBehavior? behavior)
    {
        if (_behaviorsByKey.TryGetValue(key, out var entry))
        {
            behavior = entry.Behavior;
            return true;
        }

        behavior = null;
        return false;
    }

    public IReadOnlyList<BehaviorDescriptor> List() =>
        _behaviorsByKey.Values
            .Select(entry => new BehaviorDescriptor(entry.Behavior.Key, entry.Behavior.DisplayName, entry.Source))
            .OrderBy(descriptor => descriptor.Key, StringComparer.Ordinal)
            .ToArray();

    public Result Register(IBotBehavior behavior, string source)
    {
        if (!IsCompatibleContractVersion(behavior.ContractVersion))
        {
            return new Error(
                $"Behavior \"{behavior.Key}\" targets contract version {behavior.ContractVersion}, which is "
                + $"incompatible with the platform's current contract version {BehaviorContractVersion.Current}.");
        }

        return _behaviorsByKey.TryAdd(behavior.Key, (behavior, source))
            ? Result.Ok()
            : new Error($"A behavior with key \"{behavior.Key}\" is already registered.");
    }

    /// <summary>Only the major version must match — minor/patch bumps to the SDK contract stay compatible.</summary>
    private static bool IsCompatibleContractVersion(string contractVersion) =>
        MajorVersion(contractVersion) == MajorVersion(BehaviorContractVersion.Current);

    private static int MajorVersion(string version)
    {
        var separatorIndex = version.IndexOf('.');
        var majorPart = separatorIndex < 0 ? version : version[..separatorIndex];
        return int.TryParse(majorPart, out var major) ? major : -1;
    }
}