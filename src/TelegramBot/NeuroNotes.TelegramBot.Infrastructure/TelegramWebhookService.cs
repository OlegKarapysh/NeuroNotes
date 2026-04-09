using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace NeuroNotes.TelegramBot.Infrastructure;

public sealed class TelegramWebhookService(
    ITelegramBotClient botClient,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramWebhookService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var webhookUrl = telegramOptions.Value.WebhookUrl ?? throw new Exception("Telegram Webhook URL is missing");
        await botClient.SetWebhook(url: webhookUrl, allowedUpdates: [], cancellationToken: cancellationToken);
        logger.LogInformation("Telegram webhook set to {WebhookUrl}", webhookUrl);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await botClient.DeleteWebhook(cancellationToken: cancellationToken);
        logger.LogInformation("Telegram webhook deleted");
    }
}
