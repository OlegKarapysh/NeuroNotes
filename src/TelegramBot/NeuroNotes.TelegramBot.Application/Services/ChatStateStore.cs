using System.Collections.Concurrent;
using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application.Services;

public sealed class ChatStateStore : IChatStateStore
{
    private readonly ConcurrentDictionary<long, ChatState> _store = new();

    public ChatState Get(long chatId) => _store.GetValueOrDefault(chatId, ChatState.Initial);

    public void Set(long chatId, ChatState state) => _store[chatId] = state;
}
