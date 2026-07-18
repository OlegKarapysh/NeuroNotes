namespace NeuroNotes.Platform.Public.Bots;

public enum BotStatus
{
    /// <summary>Registered, its receiver running, and healthy.</summary>
    Active,

    /// <summary>Registered but its receiver is stopped by the operator.</summary>
    Disabled,

    /// <summary>Running but repeatedly erroring; the platform keeps retrying at normal cadence (FR-022).</summary>
    Failing
}