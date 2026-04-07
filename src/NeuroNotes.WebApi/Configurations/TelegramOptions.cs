namespace NeuroNotes.WebApi.Configurations;

public sealed record TelegramOptions
{
    public const string SectionName = "Telegram";

    [Required]
    public string TelegramBotSecretToken { get; init; } = string.Empty;

    public string? WebhookUrl { get; init; }
}