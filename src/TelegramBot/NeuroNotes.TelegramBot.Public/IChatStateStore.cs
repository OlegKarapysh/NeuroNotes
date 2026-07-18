namespace NeuroNotes.TelegramBot.Public;

public interface IChatStateStore
{
    Task<ChatState> GetAsync(long botId, long chatId, CancellationToken cancellationToken = default);
    Task SetAsync(long botId, long chatId, ChatState state, CancellationToken cancellationToken = default);
}