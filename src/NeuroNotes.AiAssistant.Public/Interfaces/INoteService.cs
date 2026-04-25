using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

public interface INoteService
{
    Task<Result<Stream>> CreateNote(long userId, string text, CancellationToken cancellationToken = default);
}