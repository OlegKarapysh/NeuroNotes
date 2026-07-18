namespace NeuroNotes.Platform.Persistence.Entities;

public sealed class BotRegistrationEntity
{
    public long Id { get; set; }
    public long TelegramBotId { get; set; }
    public string? Username { get; set; }
    public required string Label { get; set; }
    public required string BehaviorKey { get; set; }
    public required byte[] EncryptedToken { get; set; }
    public BotStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}