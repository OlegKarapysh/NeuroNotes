using Whisper.net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .ConfigureTelegramOptions()
    .AddTelegramBot();

builder.Services
    .ConfigureAudioConversionOptions()
    .AddAudioConversion();

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

    try
    {
        const string whisperModelFileName = "ggml-base.bin";

        if (!File.Exists(whisperModelFileName))
        {
            app.Logger.LogInformation("Downloading Whisper model...");
            
            var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
            var ggmlModelDownloader = new WhisperGgmlDownloader(httpClient);
            
            await using var modelStream = await ggmlModelDownloader.GetGgmlModelAsync(GgmlType.Base);
            await using var fileWriter = File.OpenWrite(whisperModelFileName);
            await modelStream.CopyToAsync(fileWriter);
            
            app.Logger.LogInformation("Whisper model downloaded!");
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "An error occurred while setting webhook");
    }
});

app.MapGet("/", async ([FromServices] ITelegramBotClient telegramBotClient) => await telegramBotClient.GetMe());

app.MapPost("/telegram-bot/webhook",
    async ([FromBody] Update update,
        [FromServices] ITelegramBotClient telegramBotClient,
        [FromServices] IAudioConverter audioConverter,
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

                var fileInfo = await telegramBotClient.GetFile(update.Message.Voice.FileId);
                using var memoryStream = new MemoryStream();
                await telegramBotClient.DownloadFile(fileInfo.FilePath, memoryStream);
                memoryStream.Position = 0;

                using var wavAudioFileStream = new MemoryStream(await audioConverter.ConvertOggToWav(memoryStream.ToArray()));

                using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");
                await using var whisper = whisperFactory.CreateBuilder().WithLanguage("auto").Build();

                var transcribedText = string.Empty;

                await foreach (var result in whisper.ProcessAsync(wavAudioFileStream))
                {
                    transcribedText += result.Text;
                }

                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    await telegramBotClient.SendMessage(update.Message.Chat.Id, "Не удалось распознать речь.");
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