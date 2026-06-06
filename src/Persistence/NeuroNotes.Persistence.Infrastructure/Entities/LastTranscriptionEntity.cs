namespace NeuroNotes.Persistence.Infrastructure.Entities;

public sealed class LastTranscriptionEntity
{
    public long ChatId { get; set; }
    public required string Transcription { get; set; }
}