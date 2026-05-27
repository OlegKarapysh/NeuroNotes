using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

public interface ISpeechTextEnhancer
{
    Task<Result<string>> EnhanceText(string text, CancellationToken cancellationToken = default);
}