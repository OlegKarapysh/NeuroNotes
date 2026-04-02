using Telegram.Bot.Polling;

namespace NeuroNotes.WebApi.Telegram;

public sealed class TelegramUpdateHandler(
    IAudioConverter audioConverter,
    IWhisperProcessorFactory whisperProcessorFactory,
    ILogger<TelegramUpdateHandler> logger) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update is { Type: UpdateType.Message, Message.Text: not null })
        {
            logger.LogInformation(
                "Received message '{Text}' in a chat with ID '{ChatId}'",
                update.Message.Text,
                update.Message.Chat.Id);

            await botClient.SendMessage(update.Message.Chat.Id, update.Message.Text, cancellationToken: cancellationToken);
        }
        else if (update is { Type: UpdateType.Message, Message.Voice: not null })
        {
            await HandleVoiceMessageAsync(botClient, update.Message, cancellationToken);
        }
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error while handling Telegram update (source: {Source})", source);
        return Task.CompletedTask;
    }

    private async Task HandleVoiceMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatAction(message.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);

        var filePath = (await botClient.GetFile(message.Voice!.FileId, cancellationToken)).FilePath
                       ?? throw new InvalidOperationException("Voice message file path is missing");

        using var memoryStream = new MemoryStream();
        await botClient.DownloadFile(filePath, memoryStream, cancellationToken);

        using var wavAudioStream = new MemoryStream(await audioConverter.ConvertOggToWav(
            oggData: memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Length),
            cancellationToken: cancellationToken));

        await using var whisper = whisperProcessorFactory.Create();

        var transcribedTextBuilder = new StringBuilder();
        await foreach (var result in whisper.ProcessAsync(wavAudioStream, cancellationToken))
        {
            transcribedTextBuilder.Append(result.Text);
        }

        var transcribedText = transcribedTextBuilder.ToString();
        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            await botClient.SendMessage(message.Chat.Id, "Failed to perform speech recognition", cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(message.Chat.Id, $"\ud83c\udfa4: {transcribedText.Trim()}", cancellationToken: cancellationToken);
        }
    }
}
