namespace NeuroNotes.WebApi.Telegram;

public sealed class TelegramMessageHandler(
    ITelegramBotClient telegramBotClient,
    IAudioConverter audioConverter,
    IWhisperProcessorFactory whisperProcessorFactory) : IConsumer<Update>
{
    public async Task Consume(ConsumeContext<Update> context)
    {
        if (context.Message.Type is not UpdateType.Message)
        {
            return;
        }
        
        var message = context.Message.Message;
        if (message?.Text is not null)
        {
            await telegramBotClient.SendMessage(message.Chat.Id, message.Text);
        }
        else if (message?.Voice is not null)
        {
            await HandleVoiceMessageAsync(message);
        }
    }
    
    private async Task HandleVoiceMessageAsync(Message message)
    {
        await telegramBotClient.SendChatAction(message.Chat.Id, ChatAction.Typing);

        var filePath = (await telegramBotClient.GetFile(message.Voice!.FileId)).FilePath
                       ?? throw new InvalidOperationException("Voice message file path is missing");

        using var memoryStream = new MemoryStream();
        await telegramBotClient.DownloadFile(filePath, memoryStream);

        await using var wavAudioStream = await audioConverter.ConvertOggToWav(oggData: memoryStream);

        await using var whisper = whisperProcessorFactory.Create();

        var transcribedTextBuilder = new StringBuilder();
        await foreach (var result in whisper.ProcessAsync(wavAudioStream))
        {
            transcribedTextBuilder.Append(result.Text);
        }

        var transcribedText = transcribedTextBuilder.ToString();
        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            await telegramBotClient.SendMessage(message.Chat.Id, "Failed to perform speech recognition");
        }
        else
        {
            await telegramBotClient.SendMessage(message.Chat.Id, transcribedText.Trim());
        }
    }
}