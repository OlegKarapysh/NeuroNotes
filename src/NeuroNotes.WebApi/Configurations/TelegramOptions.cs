namespace NeuroNotes.WebApi.Configurations;

public sealed record TelegramOptions
{
    public const string SectionName = "Telegram";

    [Required]
    public string TelegramBotSecretToken { get; init; } = string.Empty;

    public bool UseWebhook { get; init; }

    public string? WebhookUrl { get; init; }
}

public sealed class TelegramOptionsValidator : IValidateOptions<TelegramOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramOptions options)
    {
        if (options.UseWebhook && string.IsNullOrWhiteSpace(options.WebhookUrl))
        {
            return ValidateOptionsResult.Fail("WebhookUrl is required when UseWebhook is true.");
        }

        return ValidateOptionsResult.Success;
    }
}