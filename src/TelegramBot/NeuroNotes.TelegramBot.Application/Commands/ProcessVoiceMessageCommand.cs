namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ProcessVoiceMessageCommand(Message VoiceMessage);

public sealed class ProcessVoiceMessageCommandHandler(
    ITelegramBotClient telegramBotClient,
    IVoiceEnhanceTranscriber voiceTranscriber,
    ILastTranscriptionStore lastTranscriptionStore,
    ILogger<ProcessVoiceMessageCommandHandler> logger) : IConsumer<ProcessVoiceMessageCommand>
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
        
        logger.LogInformation("Voice message file descriptor downloaded successfully");

        using var memoryStream = new MemoryStream();
        await telegramBotClient.DownloadFile(filePath, memoryStream);
        
        logger.LogInformation("Voice message downloaded successfully");

        
        var transcribedTextResult = await voiceTranscriber.Transcribe(memoryStream);
        if (transcribedTextResult.IsFailed)
        {
            await telegramBotClient.SendMessage(message.Chat.Id, transcribedTextResult.Errors.First().Message);
            return;
        }

        lastTranscriptionStore.Save(message.Chat.Id, transcribedTextResult.Value);

        await telegramBotClient.SendMessage(message.Chat.Id, transcribedTextResult.Value);
    }
}