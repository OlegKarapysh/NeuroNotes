namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>A generated Markdown note: the suggested file name and the note body.</summary>
public sealed record CreatedNote(string FileName, string Markdown);

public interface INoteService
{
    /// <summary>
    /// Turns the user's text into a Markdown note (LLM formatting) <b>without persisting it</b>,
    /// so the note can be previewed before the user confirms saving it.
    /// </summary>
    Task<Result<CreatedNote>> GenerateNote(long userId, string text, CancellationToken cancellationToken = default);

    /// <summary>Persists a previously generated note to the user's note store.</summary>
    Task SaveNote(long userId, CreatedNote note, CancellationToken cancellationToken = default);
}