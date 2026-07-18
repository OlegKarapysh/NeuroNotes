namespace NeuroNotes.WebApi;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMassTransit()
        {
            return services.AddMassTransit(config =>
            {
                config.SetKebabCaseEndpointNameFormatter();
                config.AddConsumers(typeof(NeuroNotes.TelegramBot.Application.AssemblyMarker).Assembly);
                config.AddConsumers(typeof(BotUpdateRouter).Assembly);
                config.MapTelegramCommandEndpoints();
                config.UsingInMemory((context, configurator) =>
                {
                    // Sets the bot for the current message BEFORE its consumer (and any constructor-injected,
                    // bot-scoped ITelegramBotClient) is resolved from the DI scope — see BotScopeFilter (D1/D2).
                    configurator.UseBotScopeFilter(context);
                    configurator.ConfigureEndpoints(context);
                });
            });
        }
    }
}