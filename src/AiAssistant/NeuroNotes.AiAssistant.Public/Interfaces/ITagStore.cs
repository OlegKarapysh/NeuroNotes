using FluentResults;

namespace NeuroNotes.AiAssistant.Public.Interfaces;

/// <summary>
/// Stores the user's tags. Tags can later be attached to notes.
/// </summary>
public interface ITagStore
{
    Result Add(long userId, string tag);
    IReadOnlyList<string> GetAll(long userId);
}