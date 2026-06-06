namespace NeuroNotes.AudioProcessing.Public.Interfaces;

public interface IVoiceTranscriber
{
    Task<Result<string>> Transcribe(MemoryStream memoryStream);
}