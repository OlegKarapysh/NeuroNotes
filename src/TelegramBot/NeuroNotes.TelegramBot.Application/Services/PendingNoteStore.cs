namespace NeuroNotes.TelegramBot.Application.Services;

public sealed class PendingNoteStore : IPendingNoteStore
{
    private readonly ConcurrentDictionary<long, CreatedNote> _notesByChat = new();

    public void Set(long chatId, CreatedNote note) => _notesByChat[chatId] = note;

    public CreatedNote? Get(long chatId) => _notesByChat.GetValueOrDefault(chatId);

    public void Clear(long chatId) => _notesByChat.TryRemove(chatId, out _);
}