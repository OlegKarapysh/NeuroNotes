namespace NeuroNotes.AiAssistant.Public.Interfaces;

public sealed record StoredNote(string FileName, string Content, DateTime SavedAt);

public interface INoteStore
{
    void Save(long userId, string fileName, string content);
    IReadOnlyList<StoredNote> GetAll(long userId);
}
