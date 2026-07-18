namespace NeuroNotes.Platform.Application;

/// <summary>
/// Orchestrates bot fleet management for the admin API: validates a candidate bot against the behavior
/// catalog and Telegram itself before ever touching the registry, then hands the running receiver
/// over to <see cref="IBotLifecycle"/> — never both at once, so a bot is either fully registered and
/// running, or not registered at all (FR-006, FR-008, FR-010, FR-011).
/// </summary>
public sealed class BotRegistrationService(
    IBotRegistry botRegistry,
    IBehaviorCatalog behaviorCatalog,
    ITokenProtector tokenProtector,
    IBotTokenValidator botTokenValidator,
    IBotLifecycle botLifecycle)
{
    public async Task<Result<BotRegistration>> Register(
        string label, string behaviorKey, string token, CancellationToken cancellationToken = default)
    {
        if (!behaviorCatalog.TryGet(behaviorKey, out _))
        {
            return new Error($"Unknown behavior \"{behaviorKey}\".");
        }

        var validation = await botTokenValidator.Validate(token, cancellationToken);
        if (validation.IsFailed)
        {
            return new Error(validation.Errors.First().Message);
        }

        var (telegramBotId, username) = validation.Value;
        var encryptedToken = tokenProtector.Protect(token);

        var addResult = await botRegistry.AddAsync(telegramBotId, username, label, behaviorKey, encryptedToken, cancellationToken);
        if (addResult.IsFailed)
        {
            return addResult;
        }

        var registration = addResult.Value;
        System.Diagnostics.Activity.Current?.SetTag("bot.id", registration.Id);
        await botLifecycle.StartAsync(registration.Id, token, cancellationToken);

        return registration;
    }

    public Task<IReadOnlyList<BotRegistration>> List(CancellationToken cancellationToken = default) =>
        botRegistry.ListAsync(cancellationToken);

    public Task<BotRegistration?> Get(long botId, CancellationToken cancellationToken = default) =>
        botRegistry.GetAsync(botId, cancellationToken);

    public async Task<Result> Disable(long botId, CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Activity.Current?.SetTag("bot.id", botId);
        var updateResult = await botRegistry.UpdateStatusAsync(botId, BotStatus.Disabled, cancellationToken);
        if (updateResult.IsFailed)
        {
            return updateResult;
        }

        await botLifecycle.StopAsync(botId, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> Enable(long botId, CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Activity.Current?.SetTag("bot.id", botId);
        var encryptedToken = await botRegistry.GetEncryptedTokenAsync(botId, cancellationToken);
        if (encryptedToken is null)
        {
            return new Error($"Bot {botId} was not found.");
        }

        var updateResult = await botRegistry.UpdateStatusAsync(botId, BotStatus.Active, cancellationToken);
        if (updateResult.IsFailed)
        {
            return updateResult;
        }

        var token = tokenProtector.Unprotect(encryptedToken);
        await botLifecycle.StartAsync(botId, token, cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> RotateToken(long botId, string newToken, CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Activity.Current?.SetTag("bot.id", botId);
        var registration = await botRegistry.GetAsync(botId, cancellationToken);
        if (registration is null)
        {
            return new Error($"Bot {botId} was not found.");
        }

        var validation = await botTokenValidator.Validate(newToken, cancellationToken);
        if (validation.IsFailed)
        {
            return new Error(validation.Errors.First().Message);
        }

        var (telegramBotId, _) = validation.Value;
        var encryptedToken = tokenProtector.Protect(newToken);

        var updateResult = await botRegistry.UpdateTokenAsync(botId, telegramBotId, encryptedToken, cancellationToken);
        if (updateResult.IsFailed)
        {
            return updateResult;
        }

        await botLifecycle.StartAsync(botId, newToken, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> Remove(long botId, CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Activity.Current?.SetTag("bot.id", botId);
        var removeResult = await botRegistry.RemoveAsync(botId, cancellationToken);
        if (removeResult.IsFailed)
        {
            return removeResult;
        }

        await botLifecycle.RemoveAsync(botId, cancellationToken);
        return Result.Ok();
    }
}