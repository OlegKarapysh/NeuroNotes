namespace NeuroNotes.WebApi.Commands;

public sealed record ProcessVoiceMessageCommand(Message VoiceMessage);

public sealed class ProcessVoiceMessageCommandHandler(
    ITelegramBotClient telegramBotClient,
    IAudioConverter audioConverter,
    IWhisperProcessorFactory whisperProcessorFactory) : IConsumer<ProcessVoiceMessageCommand>
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

        await using var wavAudioStream = await audioConverter.ConvertOggToWav(oggData: memoryStream);

        await using var whisper = whisperProcessorFactory.Create();

        var transcribedTextBuilder = new StringBuilder();
        await foreach (var result in whisper.ProcessAsync(wavAudioStream))
        {
            transcribedTextBuilder.Append(result.Text);
        }

        var transcribedText = transcribedTextBuilder.ToString();
        var response = string.IsNullOrWhiteSpace(transcribedText)
            ? "Failed to perform speech recognition"
            : transcribedText.Trim();
        
        await telegramBotClient.SendMessage(message.Chat.Id, response);
    }
}