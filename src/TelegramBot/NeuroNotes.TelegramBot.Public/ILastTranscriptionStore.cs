namespace NeuroNotes.TelegramBot.Public;

public interface ILastTranscriptionStore
{
    Task SaveAsync(long botId, long chatId, string transcription, CancellationToken cancellationToken = default);
    Task<string?> GetAsync(long botId, long chatId, CancellationToken cancellationToken = default);
}