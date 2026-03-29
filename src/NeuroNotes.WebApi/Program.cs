var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .ConfigureTelegramOptions(builder.Configuration)
    .AddTelegramBot();

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
        const string webhookUrl = "https://739e-185-136-134-30.ngrok-free.app/telegram-bot/webhook";
        await telegramBotClient.SetWebhook(url: webhookUrl, allowedUpdates: []);
        
        app.Logger.LogInformation($"Webhook is set to {webhookUrl}");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "An error occurred while setting webhook");
    }
});

app.MapGet("/", async ([FromServices] ITelegramBotClient telegramBotClient) => await telegramBotClient.GetMe());

app.MapPost("/telegram-bot/webhook",
    async ([FromBody] Update update, [FromServices] ITelegramBotClient telegramBotClient, ILogger<Program> logger) =>
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message.Text: not null })
            {
                var chatId = update.Message.Chat.Id;
                var text = update.Message.Text;

                logger.LogInformation("Received message '{Text}' in a chat with ID '{ChatId}'", text, chatId);

                await telegramBotClient.SendMessage(chatId, text);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while processing update from webhook");
        }

        return Results.Ok();
    });

await app.RunAsync();