namespace NeuroNotes.Platform.Infrastructure;

/// <summary>
/// Restores every non-disabled registered bot on host startup (FR-016/SC-004) — bots added at runtime
/// survive a restart without any manual re-registration.
/// </summary>
public sealed class BotRestoreHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BotRestoreHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var botRegistry = scope.ServiceProvider.GetRequiredService<IBotRegistry>();
        var tokenProtector = scope.ServiceProvider.GetRequiredService<ITokenProtector>();
        var supervisor = scope.ServiceProvider.GetRequiredService<BotSupervisor>();

        var bots = await botRegistry.ListAsync(cancellationToken);

        foreach (var bot in bots.Where(b => b.Status != BotStatus.Disabled))
        {
            try
            {
                var encryptedToken = await botRegistry.GetEncryptedTokenAsync(bot.Id, cancellationToken);
                if (encryptedToken is null)
                {
                    logger.LogError("Bot {BotId} has no stored token; skipping restore.", bot.Id);
                    continue;
                }

                var token = tokenProtector.Unprotect(encryptedToken);
                await supervisor.StartAsync(bot.Id, token, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to restore bot {BotId} on startup.", bot.Id);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}