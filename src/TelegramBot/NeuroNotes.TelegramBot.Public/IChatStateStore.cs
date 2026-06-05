namespace NeuroNotes.TelegramBot.Public;

public interface IChatStateStore
{
    Task<ChatState> GetAsync(long chatId, CancellationToken cancellationToken = default);
    Task SetAsync(long chatId, ChatState state, CancellationToken cancellationToken = default);
}