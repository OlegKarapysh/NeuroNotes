namespace NeuroNotes.TelegramBot.Persistence.Entities;

public sealed class LastTranscriptionEntity
{
    public long BotId { get; set; }
    public long ChatId { get; set; }
    public required string Transcription { get; set; }
}