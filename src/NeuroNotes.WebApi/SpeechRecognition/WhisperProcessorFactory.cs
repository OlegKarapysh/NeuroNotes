namespace NeuroNotes.WebApi.SpeechRecognition;

public class WhisperProcessorFactory : IWhisperProcessorFactory, IDisposable
{
    private WhisperFactory? _whisperFactory;

    public WhisperProcessor Create()
    {
        if (_whisperFactory is null)
        {
            throw new Exception("Whisper processor factory not initialized");
        }

        return _whisperFactory.CreateBuilder().WithLanguage("auto").Build();
    }

    public void Initialize(string whisperModelFilePath)
    {
        _whisperFactory = WhisperFactory.FromPath(whisperModelFilePath);
    }

    public void Dispose()
    {
        _whisperFactory?.Dispose();
    }
}