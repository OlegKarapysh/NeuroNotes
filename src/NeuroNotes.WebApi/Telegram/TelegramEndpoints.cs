using Telegram.Bot.Polling;

namespace NeuroNotes.WebApi.Telegram;

public static class TelegramEndpoints
{
    public static WebApplication MapTelegramEndpoints(this WebApplication app)
    {
        var telegramOptions = app.Services.GetRequiredService<IOptions<TelegramOptions>>().Value;

        if (telegramOptions.UseWebhook)
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
                    catch (Exception ex)
                    {
                        await handler.HandleErrorAsync(botClient, ex, HandleErrorSource.HandleUpdateError, cancellationToken);
                    }

                    return Results.Ok();
                });
        }

        return app;
    }
}
