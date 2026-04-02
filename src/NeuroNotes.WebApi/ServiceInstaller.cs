using NeuroNotes.WebApi.Telegram;

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

            services.AddSingleton<IValidateOptions<TelegramOptions>, TelegramOptionsValidator>();

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

        public IServiceCollection AddTelegramUpdateHandling(IConfiguration configuration)
        {
            services.AddSingleton<TelegramUpdateHandler>();

            var useWebhook = configuration
                .GetSection(TelegramOptions.SectionName)
                .GetValue<bool>(nameof(TelegramOptions.UseWebhook));

            if (useWebhook)
            {
                services.AddHostedService<WebhookService>();
            }
            else
            {
                services.AddHostedService<PollingService>();
            }

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

        public IServiceCollection AddWhisperServices()
        {
            return services.AddSingleton<IWhisperProcessorFactory, WhisperProcessorFactory>()
                .AddSingleton<IWhisperDownloader, WhisperDownloader>();
        }
    }
}