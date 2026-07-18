namespace NeuroNotes.Platform.Infrastructure.Admin;

/// <summary>Operator-only fleet management API — see contracts/admin-api.md. Never off the end-user Telegram surface.</summary>
public static class AdminEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/admin").AddEndpointFilter<AdminApiKeyAuth>();

        group.MapPost("/bots", RegisterBot);
        group.MapGet("/bots", ListBots);
        group.MapGet("/bots/{id:long}", GetBot);
        group.MapPost("/bots/{id:long}/disable", DisableBot);
        group.MapPost("/bots/{id:long}/enable", EnableBot);
        group.MapPut("/bots/{id:long}/token", RotateToken);
        group.MapDelete("/bots/{id:long}", RemoveBot);
        group.MapGet("/behaviors", ListBehaviors);
        group.MapPost("/behaviors", UploadBehaviorExtension);
    }

    private static async Task<IResult> RegisterBot(RegisterBotRequest request, BotRegistrationService service, CancellationToken cancellationToken)
    {
        var result = await service.Register(request.Label, request.BehaviorKey, request.Token, cancellationToken);
        return result.IsFailed
            ? MapFailure(result.Errors)
            : Results.Created($"/admin/bots/{result.Value.Id}", ToResponse(result.Value));
    }

    private static async Task<IResult> ListBots(BotRegistrationService service, CancellationToken cancellationToken)
    {
        var bots = await service.List(cancellationToken);
        return Results.Ok(bots.Select(ToResponse));
    }

    private static async Task<IResult> GetBot(long id, BotRegistrationService service, CancellationToken cancellationToken)
    {
        var bot = await service.Get(id, cancellationToken);
        return bot is null ? Results.NotFound() : Results.Ok(ToResponse(bot));
    }

    private static async Task<IResult> DisableBot(long id, BotRegistrationService service, CancellationToken cancellationToken)
    {
        var result = await service.Disable(id, cancellationToken);
        return result.IsFailed ? MapFailure(result.Errors) : Results.Ok();
    }

    private static async Task<IResult> EnableBot(long id, BotRegistrationService service, CancellationToken cancellationToken)
    {
        var result = await service.Enable(id, cancellationToken);
        return result.IsFailed ? MapFailure(result.Errors) : Results.Ok();
    }

    private static async Task<IResult> RotateToken(long id, RotateTokenRequest request, BotRegistrationService service, CancellationToken cancellationToken)
    {
        var result = await service.RotateToken(id, request.Token, cancellationToken);
        return result.IsFailed ? MapFailure(result.Errors) : Results.Ok();
    }

    private static async Task<IResult> RemoveBot(long id, BotRegistrationService service, CancellationToken cancellationToken)
    {
        var result = await service.Remove(id, cancellationToken);
        return result.IsFailed ? MapFailure(result.Errors) : Results.NoContent();
    }

    private static IResult ListBehaviors(IBehaviorCatalog catalog) => Results.Ok(catalog.List());

    /// <summary>
    /// Uploads a compiled behavior-extension assembly, loads it, and registers every <see cref="IBotBehavior"/>
    /// it contains (FR-005). A bad extension is rejected without affecting the running platform or any bot (FR-006).
    /// </summary>
    private static async Task<IResult> UploadBehaviorExtension(
        IFormFile package, PluginStore pluginStore, ExtensionAssemblyLoader loader, IBehaviorCatalog catalog, CancellationToken cancellationToken)
    {
        string assemblyPath;
        await using (var stream = package.OpenReadStream())
        {
            assemblyPath = await pluginStore.SaveAsync(package.FileName, stream, cancellationToken);
        }

        var loadResult = loader.Load(assemblyPath);
        if (loadResult.IsFailed)
        {
            return Results.BadRequest(new { error = loadResult.Errors.First().Message });
        }

        var loaded = new List<string>();
        foreach (var behavior in loadResult.Value)
        {
            var registerResult = catalog.Register(behavior, $"extension:{package.FileName}");
            if (registerResult.IsFailed)
            {
                return Results.Conflict(new { error = registerResult.Errors.First().Message, loaded });
            }

            loaded.Add(behavior.Key);
        }

        return Results.Created("/admin/behaviors", new { loaded, assembly = package.FileName });
    }

    /// <summary>Maps a domain failure message to the closest HTTP status; never echoes back a token.</summary>
    private static IResult MapFailure(IReadOnlyList<IError> errors)
    {
        var message = errors.Count > 0 ? errors[0].Message : "The request could not be completed.";

        if (message.Contains("already registered", StringComparison.OrdinalIgnoreCase)
            || message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Conflict(new { error = message });
        }

        if (message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound(new { error = message });
        }

        return Results.BadRequest(new { error = message });
    }

    private static object ToResponse(BotRegistration bot) => new
    {
        botId = bot.Id,
        telegramBotId = bot.TelegramBotId,
        username = bot.Username,
        label = bot.Label,
        behaviorKey = bot.BehaviorKey,
        status = bot.Status.ToString(),
        createdAt = bot.CreatedAt,
        updatedAt = bot.UpdatedAt
    };

    private sealed record RegisterBotRequest(string Label, string BehaviorKey, string Token);

    private sealed record RotateTokenRequest(string Token);
}