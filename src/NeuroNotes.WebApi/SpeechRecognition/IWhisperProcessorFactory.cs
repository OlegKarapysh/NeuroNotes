namespace NeuroNotes.WebApi.SpeechRecognition;

public interface IWhisperProcessorFactory
{
    WhisperProcessor Create(string language = "auto");
}