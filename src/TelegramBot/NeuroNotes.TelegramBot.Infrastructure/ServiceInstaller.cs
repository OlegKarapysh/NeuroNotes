namespace NeuroNotes.TelegramBot.Infrastructure;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTelegramBotModule(IWebHostEnvironment environment)
        {
            services.ConfigureTelegramOptions().ConfigureBusOptions().AddTelegramBot(environment);

            services.AddSingleton<IPendingGitHubLinkStore, PendingGitHubLinkStore>();

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

        public IServiceCollection ConfigureBusOptions()
        {
            services.AddOptions<BusOptions>()
                .BindConfiguration(BusOptions.SectionName)
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

        EndpointConvention.Map<PushNoteToGitHubCommand>(
            destinationAddress: new Uri($"queue:{nameof(PushNoteToGitHubCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<ConnectGitHubCommand>(
            destinationAddress: new Uri($"queue:{nameof(ConnectGitHubCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<ProcessTextMessageCommand>(
            destinationAddress: new Uri($"queue:{nameof(ProcessTextMessageCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<EditTranscriptionCommand>(
            destinationAddress: new Uri($"queue:{nameof(EditTranscriptionCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<AddTagCommand>(
            destinationAddress: new Uri($"queue:{nameof(AddTagCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<ListTagsCommand>(
            destinationAddress: new Uri($"queue:{nameof(ListTagsCommandHandler).ToKebabCase()}"));
    }
}