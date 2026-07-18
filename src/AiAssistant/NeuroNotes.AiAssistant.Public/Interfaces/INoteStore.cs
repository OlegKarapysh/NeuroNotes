namespace NeuroNotes.AiAssistant.Public.Interfaces;

public sealed record StoredNote(string FileName, string Content, DateTime SavedAt);

public interface INoteStore
{
    /// <summary>
    /// Persists a note and associates it with the given tags. Only tags that already exist for the
    /// user (within the same bot's data context) are linked; unknown tag names are ignored.
    /// </summary>
    Task SaveAsync(long botId, long userId, string fileName, string content, IReadOnlyList<string> tags, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredNote>> GetAllAsync(long botId, long userId, CancellationToken cancellationToken = default);
}