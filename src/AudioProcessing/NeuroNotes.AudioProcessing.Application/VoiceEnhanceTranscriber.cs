using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AudioProcessing.Application;

public sealed class VoiceEnhanceTranscriber(
    IVoiceTranscriber voiceTranscriber,
    ISpeechTextEnhancer speechTextEnhancer) : IVoiceEnhanceTranscriber
{
    public async Task<Result<string>> Transcribe(MemoryStream memoryStream)
    {
        var transcriptionResult = await voiceTranscriber.Transcribe(memoryStream);
        if (transcriptionResult.IsFailed)
        {
            return transcriptionResult;
        }

        var result = await speechTextEnhancer.EnhanceText(transcriptionResult.Value);

        return result;
    }
}
