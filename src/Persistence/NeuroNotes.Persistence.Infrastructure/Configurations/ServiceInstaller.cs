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
                // Retry on transient failures so the app tolerates the DB not being ready yet
                // (e.g. after a droplet reboot, when restart policies bring containers up out of order)
                // and brief Postgres restarts.
                options.UseNpgsql(persistenceOptions.ConnectionString, npgsql => npgsql.EnableRetryOnFailure());
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