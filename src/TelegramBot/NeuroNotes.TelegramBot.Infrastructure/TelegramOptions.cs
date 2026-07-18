namespace NeuroNotes.TelegramBot.Infrastructure;

public sealed record TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>
    /// Legacy bootstrap token for the single bot that ran before the platform existed. Optional: if
    /// present, the <c>migrate</c> step seeds it as the platform's first bot registration exactly once
    /// (FR-024); a fresh deployment can be entirely admin-API-driven and omit this.
    /// </summary>
    public string? TelegramBotSecretToken { get; init; }
}