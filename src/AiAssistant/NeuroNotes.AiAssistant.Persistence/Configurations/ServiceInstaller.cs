namespace NeuroNotes.AiAssistant.Persistence.Configurations;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAiAssistantPersistence()
        {
            services.ConfigurePersistenceOptions();

            services.AddDbContext<AiAssistantDbContext>((serviceProvider, options) =>
            {
                var persistenceOptions = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
                // Retry on transient failures so the app tolerates the DB not being ready yet
                // (e.g. after a droplet reboot, when restart policies bring containers up out of order)
                // and brief Postgres restarts.
                options.UseNpgsql(persistenceOptions.ConnectionString, npgsql => npgsql.EnableRetryOnFailure());
            });

            // Expose the context as a base DbContext too, so the host's `migrate` command can
            // discover and migrate every module's context via GetServices<DbContext>().
            services.AddScoped<DbContext>(serviceProvider => serviceProvider.GetRequiredService<AiAssistantDbContext>());

            services.AddScoped<INoteStore, PostgresNoteStore>();
            services.AddScoped<ITagStore, PostgresTagStore>();

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