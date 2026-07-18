namespace NeuroNotes.Platform.Infrastructure;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPlatformModule()
        {
            services.ConfigurePlatformOptions();

            services.AddPlatformPersistence();

            services.AddDataProtection()
                .PersistKeysToDbContext<PlatformDbContext>();
            services.AddSingleton<ITokenProtector, DataProtectionTokenProtector>();

            services.AddHttpClient();
            services.AddSingleton<IBotClientRegistry, BotClientRegistry>();
            services.AddSingleton<IBotTokenValidator, TelegramBotTokenValidator>();

            // Scoped so it resolves the CURRENT message's bot — see BotContextAccessor/BotScopeFilter (D1/D2).
            // Existing command handlers keep injecting ITelegramBotClient unchanged; this registration is
            // what makes that injection resolve to the correct bot's client.
            services.AddScoped<BotContextAccessor>();
            services.AddScoped<IBotContext>(serviceProvider => serviceProvider.GetRequiredService<BotContextAccessor>());
            services.AddScoped<ITelegramBotClient>(serviceProvider =>
                serviceProvider.GetRequiredService<IBotClientRegistry>()
                    .Get(serviceProvider.GetRequiredService<IBotContext>().BotId));

            services.AddSingleton<IBehaviorCatalog, BehaviorCatalog>();
            services.AddScoped<BotRegistrationService>();
            services.AddScoped<BotHealthTracker>();

            services.AddSingleton<WebhookSecretProvider>();
            services.AddSingleton<PollingBotReceiver>();
            services.AddScoped<WebhookBotReceiver>();

            services.AddSingleton<BotSupervisor>();
            services.AddSingleton<IBotLifecycle>(serviceProvider => serviceProvider.GetRequiredService<BotSupervisor>());
            services.AddHostedService<BotRestoreHostedService>();

            services.AddSingleton<PluginStore>();
            services.AddSingleton<ExtensionAssemblyLoader>();

            return services;
        }

        public IServiceCollection ConfigurePlatformOptions()
        {
            services.AddOptions<PlatformOptions>()
                .BindConfiguration(PlatformOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }

    /// <summary>Maps the operator-only admin API (SEC-005) — see contracts/admin-api.md.</summary>
    public static WebApplication MapAdminApi(this WebApplication app)
    {
        AdminEndpoints.Map(app);
        return app;
    }

    /// <summary>Maps the per-bot webhook endpoint used outside Development — see <see cref="BotSupervisor"/>.</summary>
    public static WebApplication MapBotWebhook(this WebApplication app)
    {
        app.MapPost("/telegram-bot/webhook/{botId:long}", async (
            long botId,
            Update update,
            HttpRequest request,
            IBotRegistry botRegistry,
            WebhookSecretProvider webhookSecretProvider,
            WebhookBotReceiver receiver,
            CancellationToken cancellationToken) =>
        {
            // Validate the secret first (constant-time) and independently of whether the bot exists, so an
            // unauthenticated caller always sees Unauthorized and cannot enumerate which bot ids are
            // registered (NotFound vs Unauthorized) — the secret is derived from the bot id, not stored.
            var providedSecret = request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
            if (!FixedTimeEquals(providedSecret, webhookSecretProvider.GetSecret(botId)))
            {
                return Results.Unauthorized();
            }

            // A missing or Disabled bot must not process updates — Telegram may still retry a webhook for a
            // bot that was just disabled (its webhook is deleted, but retries are already in flight).
            var registration = await botRegistry.GetAsync(botId, cancellationToken);
            if (registration is null || registration.Status == BotStatus.Disabled)
            {
                return Results.NotFound();
            }

            await receiver.HandleAsync(botId, update, cancellationToken);
            return Results.Ok();
        });

        return app;
    }

    /// <summary>Registers the bot-scope consume filter (see <see cref="BotScopeFilter{T}"/>) on the bus.</summary>
    public static void UseBotScopeFilter(this IConsumePipeConfigurator configurator, IRegistrationContext context) =>
        configurator.UseConsumeFilter(typeof(BotScopeFilter<>), context);

    /// <summary>Constant-time comparison of the webhook secret, mirroring <see cref="AdminApiKeyAuth"/>.</summary>
    private static bool FixedTimeEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return providedBytes.Length == expectedBytes.Length
               && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}