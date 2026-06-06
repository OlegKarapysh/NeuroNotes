namespace NeuroNotes.TelegramBot.Public;

public interface ILastTranscriptionStore
{
    Task SaveAsync(long chatId, string transcription, CancellationToken cancellationToken = default);
    Task<string?> GetAsync(long chatId, CancellationToken cancellationToken = default);
}