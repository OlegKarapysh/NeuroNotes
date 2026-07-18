using Grafana.OpenTelemetry;
using NeuroNotes.TelegramBot.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Liveness probe: no registered checks, so `/health` returns 200 without touching Telegram,
// OpenAI, or GitHub. The container healthcheck and the deploy workflow's post-deploy poll use it.
builder.Services.AddHealthChecks();

builder.Services.AddMassTransit();

builder.Services.AddPlatformModule();
builder.Services.AddAudioProcessingModule();
builder.Services.AddTelegramBotModule();
builder.Services.AddAiAssistantModule();
builder.Services.AddGitHubModule();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.UseGrafana())
    .WithMetrics(m => m.UseGrafana());
builder.Logging.AddOpenTelemetry(o => o.UseGrafana());

var app = builder.Build();

// `dotnet NeuroNotes.WebApi.dll migrate` applies pending EF migrations and exits without
// starting the host. The deploy workflow runs this in a one-off container before each rollout.
// Each module owns its own DbContext (registered as a base DbContext too), so migrate every one.
if (args.Contains("migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    foreach (var dbContext in scope.ServiceProvider.GetServices<DbContext>())
    {
        await dbContext.Database.MigrateAsync();
    }

    // One-time continuity step (FR-024): if the pre-platform single-bot token is still configured and no
    // bot has been registered yet, seed it as the platform's first bot and backfill every existing row to it.
    await SeedLegacyBotAsync(scope.ServiceProvider);

    return;
}

// Registers the platform's one built-in behavior, then reloads every previously-uploaded behavior
// extension (FR-005). Runs before the host starts serving traffic/receivers, so every bot's assigned
// behavior is already present by the time BotUpdateRouter looks it up.
using (var startupScope = app.Services.CreateScope())
{
    var behaviorCatalog = startupScope.ServiceProvider.GetRequiredService<IBehaviorCatalog>();
    var noteCaptureBehavior = startupScope.ServiceProvider.GetRequiredService<NoteCaptureBehavior>();
    behaviorCatalog.Register(noteCaptureBehavior, "built-in");

    var pluginStore = startupScope.ServiceProvider.GetRequiredService<PluginStore>();
    var extensionLoader = startupScope.ServiceProvider.GetRequiredService<ExtensionAssemblyLoader>();
    var startupLogger = startupScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BehaviorExtensionRestore");

    foreach (var assemblyPath in pluginStore.ListStoredAssemblyPaths())
    {
        var loadResult = extensionLoader.Load(assemblyPath);
        if (loadResult.IsFailed)
        {
            startupLogger.LogError("Failed to reload behavior extension {Path}: {Error}", assemblyPath, loadResult.Errors.First().Message);
            continue;
        }

        foreach (var behavior in loadResult.Value)
        {
            var registerResult = behaviorCatalog.Register(behavior, $"extension:{Path.GetFileName(assemblyPath)}");
            if (registerResult.IsFailed)
            {
                startupLogger.LogError("Failed to register behavior from {Path}: {Error}", assemblyPath, registerResult.Errors.First().Message);
            }
        }
    }
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.MapAdminApi();
app.MapBotWebhook();

await app.RunAsync();

/// <summary>
/// Seeds the platform's first bot registration from the legacy <c>Telegram:TelegramBotSecretToken</c>
/// (if configured) and backfills every pre-existing <c>BotId</c> column (added with a temporary default of
/// 0 by each module's migration) to that bot's id. Idempotent: only runs while no bot is registered yet, so
/// re-running <c>migrate</c> after the operator has added more bots is a no-op.
/// </summary>
static async Task SeedLegacyBotAsync(IServiceProvider services)
{
    var telegramOptions = services.GetRequiredService<IOptions<TelegramOptions>>().Value;
    if (string.IsNullOrWhiteSpace(telegramOptions.TelegramBotSecretToken))
    {
        return;
    }

    var botRegistry = services.GetRequiredService<IBotRegistry>();
    var existingBots = await botRegistry.ListAsync();
    if (existingBots.Count > 0)
    {
        return;
    }

    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("LegacyBotSeed");

    var validator = services.GetRequiredService<IBotTokenValidator>();
    var validation = await validator.Validate(telegramOptions.TelegramBotSecretToken);
    if (validation.IsFailed)
    {
        logger.LogError("Could not validate the legacy Telegram bot token; skipping first-bot seed: {Error}", validation.Errors.First().Message);
        return;
    }

    var (telegramBotId, username) = validation.Value;
    var tokenProtector = services.GetRequiredService<ITokenProtector>();
    var encryptedToken = tokenProtector.Protect(telegramOptions.TelegramBotSecretToken);

    var addResult = await botRegistry.AddAsync(telegramBotId, username, "Legacy bot", "note-capture", encryptedToken);
    if (addResult.IsFailed)
    {
        logger.LogError("Failed to seed the legacy bot: {Error}", addResult.Errors.First().Message);
        return;
    }

    var botId = addResult.Value.Id;
    logger.LogInformation("Seeded the pre-platform bot as bot {BotId} ({Username})", botId, username);

    var telegramBotDbContext = services.GetRequiredService<TelegramBotDbContext>();
    await telegramBotDbContext.ChatStates.Where(c => c.BotId == 0).ExecuteUpdateAsync(s => s.SetProperty(c => c.BotId, botId));
    await telegramBotDbContext.LastTranscriptions.Where(t => t.BotId == 0).ExecuteUpdateAsync(s => s.SetProperty(t => t.BotId, botId));

    var aiAssistantDbContext = services.GetRequiredService<NeuroNotes.AiAssistant.Persistence.DbContexts.AiAssistantDbContext>();
    await aiAssistantDbContext.Notes.Where(n => n.BotId == 0).ExecuteUpdateAsync(s => s.SetProperty(n => n.BotId, botId));
    await aiAssistantDbContext.Tags.Where(t => t.BotId == 0).ExecuteUpdateAsync(s => s.SetProperty(t => t.BotId, botId));

    var gitHubDbContext = services.GetRequiredService<NeuroNotes.GitHub.Persistence.GitHubDbContext>();
    await gitHubDbContext.UserGitHubSettings.Where(s => s.BotId == 0).ExecuteUpdateAsync(s => s.SetProperty(x => x.BotId, botId));

    logger.LogInformation("Backfilled existing data to bot {BotId}.", botId);
}