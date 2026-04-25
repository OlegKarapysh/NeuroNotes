using Microsoft.Extensions.Logging;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AudioProcessing.Application;

public sealed class VoiceEnhanceTranscriber(
    IVoiceTranscriber voiceTranscriber,
    ISpeechTextEnhancer speechTextEnhancer,
    ILogger<VoiceEnhanceTranscriber> logger) : IVoiceEnhanceTranscriber
{
    public async Task<Result<string>> Transcribe(MemoryStream memoryStream)
    {
        var transcriptionResult = await voiceTranscriber.Transcribe(memoryStream);
        if (transcriptionResult.IsFailed)
        {
            return transcriptionResult;
        }

        var result = await speechTextEnhancer.EnhanceText(transcriptionResult.Value);
        
        logger.LogInformation("Voice message enhanced successfully");


        return result;
    }
}
