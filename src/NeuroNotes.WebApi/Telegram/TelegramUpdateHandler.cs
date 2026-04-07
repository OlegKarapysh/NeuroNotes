namespace NeuroNotes.WebApi.Telegram;

public sealed class TelegramUpdateHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<TelegramUpdateHandler> logger) : IUpdateHandler
{
    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        return publishEndpoint.Publish(message: update, cancellationToken);
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
}
