namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ProcessVoiceMessageCommand(Message VoiceMessage);

public sealed class ProcessVoiceMessageCommandHandler(
    ITelegramBotClient telegramBotClient,
    IVoiceTranscriber voiceTranscriber) : IConsumer<ProcessVoiceMessageCommand>
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

        var response = transcribedTextResult.IsFailed
            ? transcribedTextResult.Errors.First().Message
            : transcribedTextResult.Value;
        
        await telegramBotClient.SendMessage(message.Chat.Id, response);
    }
}