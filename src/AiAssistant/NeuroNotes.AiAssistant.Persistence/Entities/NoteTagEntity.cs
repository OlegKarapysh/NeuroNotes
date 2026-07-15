namespace NeuroNotes.AiAssistant.Persistence.Entities;

/// <summary>Join row associating a <see cref="NoteEntity"/> with a <see cref="TagEntity"/> (many-to-many).</summary>
public sealed class NoteTagEntity
{
    public long NoteId { get; set; }
    public long TagId { get; set; }
}