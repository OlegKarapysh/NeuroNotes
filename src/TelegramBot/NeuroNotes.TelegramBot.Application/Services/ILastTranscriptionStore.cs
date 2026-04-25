namespace NeuroNotes.TelegramBot.Application.Services;

public interface ILastTranscriptionStore
{
    void Save(long chatId, string transcription);
    string? Get(long chatId);
}
