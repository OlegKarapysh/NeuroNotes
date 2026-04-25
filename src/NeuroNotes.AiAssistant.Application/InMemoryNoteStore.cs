using System.Collections.Concurrent;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class InMemoryNoteStore : INoteStore
{
    private readonly ConcurrentDictionary<long, List<StoredNote>> _notes = new();

    public void Save(long userId, string fileName, string content)
    {
        var note = new StoredNote(fileName, content, DateTime.UtcNow);
        var notes = _notes.GetOrAdd(userId, _ => []);
        lock (notes)
        {
            notes.Add(note);
        }
    }

    public IReadOnlyList<StoredNote> GetAll(long userId)
    {
        if (!_notes.TryGetValue(userId, out var notes))
        {
            return [];
        }

        lock (notes)
        {
            return notes.ToArray();
        }
    }
}
