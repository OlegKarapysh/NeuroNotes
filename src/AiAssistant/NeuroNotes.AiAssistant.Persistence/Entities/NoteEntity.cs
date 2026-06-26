namespace NeuroNotes.AiAssistant.Persistence.Entities;

public sealed class NoteEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string FileName { get; set; }
    public required string Content { get; set; }
    public DateTime SavedAt { get; set; }
}