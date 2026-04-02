namespace NeuroNotes.WebApi.Telegram;

public sealed class WebhookService(
    ITelegramBotClient botClient,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<WebhookService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var webhookUrl = telegramOptions.Value.WebhookUrl!;
        await botClient.SetWebhook(url: webhookUrl, allowedUpdates: [], cancellationToken: cancellationToken);
        logger.LogInformation("Telegram webhook set to {WebhookUrl}", webhookUrl);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await botClient.DeleteWebhook(cancellationToken: cancellationToken);
        logger.LogInformation("Telegram webhook deleted");
    }
}
