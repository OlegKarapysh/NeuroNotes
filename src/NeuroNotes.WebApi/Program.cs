using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .ConfigureTelegramOptions()
    .AddTelegramBot();

builder.Services
    .ConfigureAudioConversionOptions()
    .AddAudioConversion();

builder.Services.AddWhisperServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    try
    {
        var telegramBotClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var telegramOptions = scope.ServiceProvider.GetRequiredService<IOptions<TelegramOptions>>();
        await telegramBotClient.SetWebhook(url: telegramOptions.Value.WebhookUrl, allowedUpdates: []);
        
        app.Logger.LogInformation($"Webhook is set to {telegramOptions.Value.WebhookUrl}");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "An error occurred while setting webhook");
    }

    var whisperDownloader = scope.ServiceProvider.GetRequiredService<IWhisperDownloader>();
    var whisperProcessorFactory = scope.ServiceProvider.GetRequiredService<IWhisperProcessorFactory>();
    
    await whisperDownloader.DownloadWhisper();
    whisperProcessorFactory.Initialize(whisperModelFilePath: IWhisperDownloader.WhisperModelFileName);
});

app.MapGet("/", async ([FromServices] ITelegramBotClient telegramBotClient) => await telegramBotClient.GetMe());

app.MapPost("/telegram-bot/webhook",
    async ([FromBody] Update update,
        [FromServices] ITelegramBotClient telegramBotClient,
        [FromServices] IAudioConverter audioConverter,
        [FromServices] IWhisperProcessorFactory  whisperProcessorFactory,
        ILogger<Program> logger) =>
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message.Text: not null })
            {
                logger.LogInformation("Received message '{Text}' in a chat with ID '{ChatId}'", update.Message.Text, update.Message.Chat.Id);

                await telegramBotClient.SendMessage(update.Message.Chat.Id, update.Message.Text);
            }
            else if (update is { Type: UpdateType.Message, Message.Voice: not null })
            {
                await telegramBotClient.SendChatAction(update.Message.Chat.Id, ChatAction.Typing);

                var filePath = (await telegramBotClient.GetFile(update.Message.Voice.FileId)).FilePath
                               ?? throw new Exception("Voice message file path is missing");
                
                using var memoryStream = new MemoryStream();
                await telegramBotClient.DownloadFile(filePath, memoryStream);
                memoryStream.Position = 0;

                using var wavAudioFileStream = new MemoryStream(await audioConverter.ConvertOggToWav(memoryStream.ToArray()));

                await using var whisper = whisperProcessorFactory.Create();

                var transcribedTextBuilder = new StringBuilder();
                await foreach (var result in whisper.ProcessAsync(wavAudioFileStream))
                {
                    transcribedTextBuilder.Append(result.Text);
                }

                var transcribedText = transcribedTextBuilder.ToString();
                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    await telegramBotClient.SendMessage(update.Message.Chat.Id, "Failed to perform speech recognition");
                }
                else
                {
                    await telegramBotClient.SendMessage(update.Message.Chat.Id, $"🎤: {transcribedText.Trim()}");
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while processing update from webhook");
        }

        return Results.Ok();
    });

await app.RunAsync();