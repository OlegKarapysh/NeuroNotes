using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

public interface INoteService
{
    Task<Result<Stream>> CreateNote(string text, CancellationToken cancellationToken = default);
}