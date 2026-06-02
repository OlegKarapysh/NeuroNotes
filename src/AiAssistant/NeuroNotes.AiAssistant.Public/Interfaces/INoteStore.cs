namespace NeuroNotes.AiAssistant.Public.Interfaces;

public sealed record StoredNote(string FileName, string Content, DateTime SavedAt);

public interface INoteStore
{
    Task SaveAsync(long userId, string fileName, string content, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredNote>> GetAllAsync(long userId, CancellationToken cancellationToken = default);
}