namespace NeuroNotes.WebApi.SpeechRecognition;

public sealed class WhisperProcessorFactory(IWhisperDownloader whisperDownloader) : IWhisperProcessorFactory, IDisposable
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

    public async Task Initialize()
    {
        await whisperDownloader.DownloadWhisper();
        _whisperFactory = WhisperFactory.FromPath(IWhisperDownloader.WhisperModelFileName);
    }

    public void Dispose()
    {
        _whisperFactory?.Dispose();
    }
}