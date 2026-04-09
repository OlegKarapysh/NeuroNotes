using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeuroNotes.TelegramBot.Application;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace NeuroNotes.TelegramBot.Infrastructure;

public sealed class TelegramPollingService(
    ITelegramBotClient telegramBotClient,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TelegramPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await telegramBotClient.DeleteWebhook(cancellationToken: stoppingToken);

        await telegramBotClient.ReceiveAsync(
            updateHandler: HandleUpdate,
            errorHandler: HandleError,
            receiverOptions: new ReceiverOptions
            {
                AllowedUpdates = [],
                DropPendingUpdates = true,
            },
            cancellationToken: stoppingToken);
        
        logger.LogInformation("Telegram long-polling started");
    }

    private async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();
        await handler.HandleUpdateAsync(botClient, update, cancellationToken);
    }

    private async Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();
        await handler.HandleErrorAsync(botClient, exception, source: HandleErrorSource.PollingError, cancellationToken);
    }
}
