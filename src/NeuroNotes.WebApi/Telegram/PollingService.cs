using Telegram.Bot.Polling;

namespace NeuroNotes.WebApi.Telegram;

public sealed class PollingService(
    ITelegramBotClient botClient,
    TelegramUpdateHandler updateHandler,
    ILogger<PollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Telegram long-polling...");

        await botClient.DeleteWebhook(cancellationToken: stoppingToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
            DropPendingUpdates = true,
        };

        await botClient.ReceiveAsync(
            updateHandler,
            receiverOptions,
            stoppingToken);
    }
}
