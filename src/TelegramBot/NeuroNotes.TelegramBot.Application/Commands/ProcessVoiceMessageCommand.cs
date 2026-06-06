namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ProcessVoiceMessageCommand(Message VoiceMessage);

public sealed class ProcessVoiceMessageCommandHandler(
    ITelegramBotClient telegramBotClient,
    IVoiceEnhanceTranscriber voiceTranscriber,
    ILastTranscriptionStore lastTranscriptionStore,
    IChatStateStore chatStateStore) : IConsumer<ProcessVoiceMessageCommand>
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
            await telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: transcribedTextResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(message.Chat.Id, context.CancellationToken)));
            return;
        }

        await lastTranscriptionStore.SaveAsync(message.Chat.Id, transcribedTextResult.Value, context.CancellationToken);
        await chatStateStore.SetAsync(message.Chat.Id, ChatState.HasTranscription, context.CancellationToken);

        await telegramBotClient.SendMessage(
            chatId: message.Chat.Id,
            text: transcribedTextResult.Value,
            replyMarkup: MenuKeyboardFactory.Build(ChatState.HasTranscription));
    }
}