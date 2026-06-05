namespace NeuroNotes.Persistence.Infrastructure.Configurations;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPersistenceModule()
        {
            services.ConfigurePersistenceOptions();

            services.AddDbContext<NeuroNotesDbContext>((serviceProvider, options) =>
            {
                var persistenceOptions = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
                options.UseNpgsql(persistenceOptions.ConnectionString);
            });

            services.AddScoped<INoteStore, PostgresNoteStore>();
            services.AddScoped<ITagStore, PostgresTagStore>();
            services.AddScoped<IUserGitHubSettingsStore, PostgresUserGitHubSettingsStore>();
            services.AddScoped<IChatStateStore, PostgresChatStateStore>();
            services.AddScoped<ILastTranscriptionStore, PostgresLastTranscriptionStore>();

            return services;
        }

        public IServiceCollection ConfigurePersistenceOptions()
        {
            services.AddOptions<PersistenceOptions>()
                .BindConfiguration(PersistenceOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}