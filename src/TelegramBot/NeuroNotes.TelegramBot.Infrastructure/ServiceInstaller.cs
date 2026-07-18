namespace NeuroNotes.TelegramBot.Infrastructure;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTelegramBotModule()
        {
            services.ConfigureTelegramOptions();

            // Per-bot Telegram delivery (client, receivers, admin API) is owned by the Platform module;
            // this registers only the note-capture behavior's own logic.
            services.AddScoped<CommandDispatcher>();
            services.AddScoped<NoteCaptureBehavior>();

            services.AddSingleton<IPendingGitHubLinkStore, PendingGitHubLinkStore>();
            services.AddSingleton<IPendingNoteStore, PendingNoteStore>();

            services.AddTelegramBotPersistence();

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
    }

    public static void MapTelegramCommandEndpoints(this IBusRegistrationConfigurator configurator)
    {
        EndpointConvention.Map<ProcessVoiceMessageCommand>(
            destinationAddress: new Uri($"queue:{nameof(ProcessVoiceMessageCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<PreviewNoteCommand>(
            destinationAddress: new Uri($"queue:{nameof(PreviewNoteCommandHandler).ToKebabCase()}"));

        EndpointConvention.Map<ConfirmNoteCommand>(
            destinationAddress: new Uri($"queue:{nameof(ConfirmNoteCommandHandler).ToKebabCase()}"));

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