using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ProcessVoiceMessageCommand(Message VoiceMessage);

public sealed class ProcessVoiceMessageCommandHandler(
    ITelegramBotClient telegramBotClient,
    IVoiceTranscriber voiceTranscriber,
    ISpeechTextEnhancer speechTextEnhancer,
    ILastTranscriptionStore lastTranscriptionStore) : IConsumer<ProcessVoiceMessageCommand>
{
    public async Task Consume(ConsumeContext<ProcessVoiceMessageCommand> context)
    {
        var message = context.Message.VoiceMessage;
        if (message.Voice is null)
        {
            throw new ArgumentNullException(nameof(message.Voice));
        }

        await telegramBotClient.SendChatAction(message.Chat.Id, ChatAction.Typing);

        var filePath = (await telegramBotClient.GetFile(message.Voice.FileId)).FilePath
                       ?? throw new InvalidOperationException("Voice message file path is missing");

        using var memoryStream = new MemoryStream();
        await telegramBotClient.DownloadFile(filePath, memoryStream);
        
        var transcribedTextResult = await voiceTranscriber.Transcribe(memoryStream);
        if (transcribedTextResult.IsFailed)
        {
            await telegramBotClient.SendMessage(message.Chat.Id, transcribedTextResult.Errors.First().Message);
            return;
        }

        var enhancedTextResult = await speechTextEnhancer.EnhanceText(transcribedTextResult.Value);
        var response = enhancedTextResult.IsFailed
            ? enhancedTextResult.Errors.First().Message
            : enhancedTextResult.Value;

        lastTranscriptionStore.Save(message.Chat.Id, response);

        await telegramBotClient.SendMessage(message.Chat.Id, response);
    }
}