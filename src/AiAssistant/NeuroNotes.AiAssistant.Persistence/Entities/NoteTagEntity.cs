namespace NeuroNotes.AiAssistant.Persistence.Entities;

/// <summary>Join row associating a <see cref="NoteEntity"/> with a <see cref="TagEntity"/> (many-to-many).</summary>
public sealed class NoteTagEntity
{
    public long NoteId { get; set; }
    public long TagId { get; set; }

    /// <summary>
    /// Navigation to the owning note. Lets the note and its tag links be inserted in a single
    /// <c>SaveChanges</c> (the note's generated <see cref="NoteEntity.Id"/> is fixed up onto <see cref="NoteId"/>).
    /// </summary>
    public NoteEntity? Note { get; set; }
}