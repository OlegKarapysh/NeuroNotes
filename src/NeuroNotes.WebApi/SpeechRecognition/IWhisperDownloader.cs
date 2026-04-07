namespace NeuroNotes.WebApi.SpeechRecognition;

public interface IWhisperDownloader
{
    public const string WhisperModelFileName = "ggml-base.bin";
    
    Task DownloadWhisper();
}