namespace NeuroNotes.TelegramBot.Application.Services;

public sealed class PendingNoteStore : IPendingNoteStore
{
    private readonly ConcurrentDictionary<(long BotId, long ChatId), CreatedNote> _notesByBotChat = new();

    public void Set(long botId, long chatId, CreatedNote note) => _notesByBotChat[(botId, chatId)] = note;

    public CreatedNote? Get(long botId, long chatId) => _notesByBotChat.GetValueOrDefault((botId, chatId));

    public void Clear(long botId, long chatId) => _notesByBotChat.TryRemove((botId, chatId), out _);
}