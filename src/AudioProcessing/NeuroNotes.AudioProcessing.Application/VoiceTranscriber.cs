using Microsoft.Extensions.Logging;

namespace NeuroNotes.AudioProcessing.Application;

public sealed class VoiceTranscriber(IAudioConverter audioConverter, ISpeechRecognizer speechRecognizer, ILogger<VoiceTranscriber> logger)
    : IVoiceTranscriber
{
    public async Task<Result<string>> Transcribe(MemoryStream memoryStream)
    {
        var wavAudioStreamResult = await audioConverter.ConvertOggToWav(oggData: memoryStream);
        if (wavAudioStreamResult.IsFailed)
        {
            return new Error(wavAudioStreamResult.Errors.First().Message);
        }
        
        logger.LogInformation("Voice message converted to WAV successfully");


        await using var wavAudioStream = wavAudioStreamResult.Value;
        
        var result = await speechRecognizer.RecognizeSpeech(wavAudioStream);
        
        logger.LogInformation("Voice message recognized successfully");


        return result;
    }
}