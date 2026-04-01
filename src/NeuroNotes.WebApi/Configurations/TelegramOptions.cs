namespace NeuroNotes.WebApi.Configurations;

public sealed record TelegramOptions
{
    public const string SectionName = "Telegram";

    [Required]
    public string TelegramBotSecretToken { get; init; } = string.Empty;
    
    [Required]
    public string WebhookUrl { get; init; } = string.Empty;
}