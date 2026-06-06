namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>A generated Markdown note: the suggested file name and the note body.</summary>
public sealed record CreatedNote(string FileName, string Markdown);

public interface INoteService
{
    Task<Result<CreatedNote>> CreateNote(long userId, string text, CancellationToken cancellationToken = default);
}