namespace NeuroNotes.AudioProcessing.Infrastructure.SpeechRecognition;

public sealed class WhisperSpeechRecognizer(WhisperProcessorFactory whisperProcessorFactory) : ISpeechRecognizer
{
    public async Task<Result<string>> RecognizeSpeech(Stream speech, CancellationToken cancellationToken = default)
    {
        await using var whisper = whisperProcessorFactory.Create();

        var transcribedTextBuilder = new StringBuilder();
        await foreach (var result in whisper.ProcessAsync(speech, cancellationToken))
        {
            transcribedTextBuilder.Append(result.Text);
        }

        var transcribedText = transcribedTextBuilder.ToString();
        
        return string.IsNullOrWhiteSpace(transcribedText)
            ? new Error("Failed to perform speech recognition")
            : transcribedText.Trim();
    }
}