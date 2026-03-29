namespace NeuroNotes.WebApi.Configurations;

public record TelegramOptions
{
    public string? TelegramBotSecretToken { get; init; }
}