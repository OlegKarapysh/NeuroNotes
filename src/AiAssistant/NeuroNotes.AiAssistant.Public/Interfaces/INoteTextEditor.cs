using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

public interface INoteTextEditor
{
    Task<Result<string>> EditText(
        string currentText,
        string editPrompt,
        CancellationToken cancellationToken = default);
}