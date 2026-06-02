using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>
/// Stores the user's tags. Tags can later be attached to notes.
/// </summary>
public interface ITagStore
{
    Task<Result> AddAsync(long userId, string tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllAsync(long userId, CancellationToken cancellationToken = default);
}