namespace NeuroNotes.WebApi.SpeechRecognition;

public interface IWhisperProcessorFactory
{
    WhisperProcessor Create();
    Task Initialize();
}