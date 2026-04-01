namespace NeuroNotes.WebApi;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureTelegramOptions()
        {
            services.AddOptions<TelegramOptions>()
                .BindConfiguration(TelegramOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            
            return services;
        }

        public IServiceCollection AddTelegramBot()
        {
            services.AddHttpClient("TelegramBotClient")
                .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
                {
                    var token = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value.TelegramBotSecretToken
                                ?? throw new ArgumentException("TelegramBotSecretToken is required");
                
                    return new TelegramBotClient(options: new TelegramBotClientOptions(token), httpClient);
                });
        
            return services;
        }
        
        public IServiceCollection ConfigureAudioConversionOptions()
        {
            services.AddOptions<AudioConversionOptions>()
                .BindConfiguration(AudioConversionOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            
            return services;
        }

        public IServiceCollection AddAudioConversion()
        {
            return services.AddScoped<IAudioConverter, FFmpegAudioConverter>();
        }
    }
}