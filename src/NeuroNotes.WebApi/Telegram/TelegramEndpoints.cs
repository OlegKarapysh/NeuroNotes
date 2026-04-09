using NeuroNotes.TelegramBot.Application;

namespace NeuroNotes.WebApi.Telegram;

public static class TelegramEndpoints
{
    public static WebApplication MapTelegramEndpoints(this WebApplication app)
    {
        app.MapGet("/", async ([FromServices] ITelegramBotClient telegramBotClient) => await telegramBotClient.GetMe());

        if (!app.Environment.IsDevelopment())
        {
            app.MapPost("/telegram-bot/webhook",
                async ([FromBody] Update update,
                    [FromServices] ITelegramBotClient botClient,
                    [FromServices] TelegramUpdateHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        await handler.HandleUpdateAsync(botClient, update, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        await handler.HandleErrorAsync(botClient, exception, HandleErrorSource.HandleUpdateError, cancellationToken);
                    }

                    return Results.Ok();
                });
        }

        return app;
    }
}
