namespace NeuroNotes.WebApi;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureTelegramOptions(IConfiguration configuration)
        {
            const string telegramSectionName = "Telegram";
            services.Configure<TelegramOptions>(configuration.GetSection(telegramSectionName));
        
            return services;
        }

        public IServiceCollection AddTelegramBot()
        {
            services.AddHttpClient("TelegramBotClient")
                .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
                {
                    var token = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value.TelegramBotSecretToken
                                ?? throw new ArgumentException("TelegramBotSecretToken is required");
                
                    var options = new TelegramBotClientOptions(token);
                    return new TelegramBotClient(options, httpClient);
                });
        
            return services;
        }
    }
}