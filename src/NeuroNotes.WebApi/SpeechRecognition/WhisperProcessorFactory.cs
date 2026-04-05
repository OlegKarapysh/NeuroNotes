namespace NeuroNotes.WebApi.SpeechRecognition;

public sealed class WhisperProcessorFactory(IOptions<SpeechRecognitionOptions> speechRecognitionOptions)
    : IWhisperProcessorFactory, IDisposable
{
    private readonly WhisperFactory _whisperFactory = WhisperFactory.FromPath(speechRecognitionOptions.Value.ModelFileName);

    public WhisperProcessor Create(string language = "auto") => _whisperFactory.CreateBuilder().WithLanguage(language).Build();

    public void Dispose() => _whisperFactory.Dispose();
}