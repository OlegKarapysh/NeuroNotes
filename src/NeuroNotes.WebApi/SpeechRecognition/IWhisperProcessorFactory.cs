namespace NeuroNotes.WebApi.SpeechRecognition;

public interface IWhisperProcessorFactory
{
    WhisperProcessor Create();
    void Initialize(string whisperModelFilePath);
}