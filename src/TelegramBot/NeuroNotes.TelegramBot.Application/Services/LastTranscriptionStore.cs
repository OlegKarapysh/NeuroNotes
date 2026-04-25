using System.Collections.Concurrent;

namespace NeuroNotes.TelegramBot.Application.Services;

public sealed class LastTranscriptionStore : ILastTranscriptionStore
{
    private readonly ConcurrentDictionary<long, string> _store = new();

    public void Save(long chatId, string transcription) => _store[chatId] = transcription;

    public string? Get(long chatId) => _store.GetValueOrDefault(chatId);
}
