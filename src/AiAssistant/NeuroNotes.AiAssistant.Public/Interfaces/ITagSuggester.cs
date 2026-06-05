using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>
/// Suggests, for a given note, which of the user's existing tags fit its content.
/// Only tags from <c>availableTags</c> can be returned — no new tags are ever invented.
/// </summary>
public interface ITagSuggester
{
    Task<Result<IReadOnlyList<string>>> SuggestTags(
        string noteText,
        IReadOnlyList<string> availableTags,
        CancellationToken cancellationToken = default);
}