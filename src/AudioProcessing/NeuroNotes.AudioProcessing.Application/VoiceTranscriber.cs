namespace NeuroNotes.AudioProcessing.Application;

public sealed class VoiceTranscriber(IAudioConverter audioConverter, ISpeechRecognizer speechRecognizer)
    : IVoiceTranscriber
{
    public async Task<Result<string>> Transcribe(MemoryStream memoryStream)
    {
        var wavAudioStreamResult = await audioConverter.ConvertOggToWav(oggData: memoryStream);
        if (wavAudioStreamResult.IsFailed)
        {
            return new Error(wavAudioStreamResult.Errors.First().Message);
        }

        await using var wavAudioStream = wavAudioStreamResult.Value;
        
        return await speechRecognizer.RecognizeSpeech(wavAudioStream);
    }
}