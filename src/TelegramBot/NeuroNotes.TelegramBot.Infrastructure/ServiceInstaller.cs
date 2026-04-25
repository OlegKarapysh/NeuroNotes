namespace NeuroNotes.TelegramBot.Infrastructure;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTelegramBotModule(IWebHostEnvironment environment)
        {
            services.ConfigureTelegramOptions().AddTelegramBot(environment);
            
            services.AddSingleton<ILastTranscriptionStore, LastTranscriptionStore>();
            
            return services;
        }
        
        public IServiceCollection ConfigureTelegramOptions()
        {
            services.AddOptions<TelegramOptions>()
                .BindConfiguration(TelegramOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public IServiceCollection AddTelegramBot(IWebHostEnvironment environment)
        {
            services.AddScoped<TelegramUpdateHandler>();
            services.AddHttpClient("TelegramBotClient")
                .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
                {
                    var token = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value.TelegramBotSecretToken
                                ?? throw new ArgumentException("TelegramBotSecretToken is required");

                    return new TelegramBotClient(options: new TelegramBotClientOptions(token), httpClient);
                });
            
            if (environment.IsDevelopment())
            {
                services.AddHostedService<TelegramPollingService>();
            }
            else
            {
                services.AddHostedService<TelegramWebhookService>();
            }

            return services;
        }
    }

    public static void MapTelegramCommandEndpoints(this IBusRegistrationConfigurator configurator)
    {
        EndpointConvention.Map<ProcessVoiceMessageCommand>(
            destinationAddress: new Uri($"queue:{nameof(ProcessVoiceMessageCommandHandler).ToKebabCase()}"));
        
        EndpointConvention.Map<CreateNoteCommand>(
            destinationAddress: new Uri($"queue:{nameof(CreateNoteCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<ProcessTextMessageCommand>(
            destinationAddress: new Uri($"queue:{nameof(ProcessTextMessageCommandHandler).ToKebabCase()}"));
    }
}