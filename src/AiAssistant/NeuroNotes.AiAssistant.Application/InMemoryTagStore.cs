using System.Collections.Concurrent;
using FluentResults;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class InMemoryTagStore : ITagStore
{
    private readonly ConcurrentDictionary<long, HashSet<string>> _tags = new();

    public Result Add(long userId, string tag)
    {
        var tags = _tags.GetOrAdd(userId, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        lock (tags)
        {
            return tags.Add(tag)
                ? Result.Ok()
                : new Error($"Tag \"{tag}\" already exists.");
        }
    }

    public IReadOnlyList<string> GetAll(long userId)
    {
        if (!_tags.TryGetValue(userId, out var tags))
        {
            return [];
        }

        lock (tags)
        {
            return tags.ToArray();
        }
    }
}