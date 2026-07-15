namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>A generated Markdown note: the suggested file name, the note body, and the tags applied to it.</summary>
public sealed record CreatedNote(string FileName, string Markdown, IReadOnlyList<string> Tags);

public interface INoteService
{
    /// <summary>
    /// Turns the user's text into a Markdown note (LLM formatting) <b>without persisting it</b>,
    /// so the note can be previewed before the user confirms saving it. Fitting tags are selected
    /// from the user's existing tags and written into the note's YAML front matter as part of generation,
    /// so the preview shows exactly what will be saved.
    /// </summary>
    Task<Result<CreatedNote>> GenerateNote(long userId, string text, CancellationToken cancellationToken = default);

    /// <summary>Persists a previously generated note (and its tag associations) to the user's note store.</summary>
    Task SaveNote(long userId, CreatedNote note, CancellationToken cancellationToken = default);
}