namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>
/// Stores the user's tags, scoped to a bot's data context. Tags can later be attached to notes.
/// </summary>
public interface ITagStore
{
    Task<Result> AddAsync(long botId, long userId, string tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllAsync(long botId, long userId, CancellationToken cancellationToken = default);
}