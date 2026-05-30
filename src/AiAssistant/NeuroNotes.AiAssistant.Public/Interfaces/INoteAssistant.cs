using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

public interface INoteAssistant
{
    Task<Result<string>> Ask(long userId, string question, CancellationToken cancellationToken = default);
}
