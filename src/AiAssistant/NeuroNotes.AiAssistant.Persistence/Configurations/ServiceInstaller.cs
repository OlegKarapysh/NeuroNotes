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
                options.UseNpgsql(persistenceOptions.ConnectionString, npgsql => npgsql.EnableRetryOnFailure());
            });

            services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(serviceProvider => serviceProvider.GetRequiredService<AiAssistantDbContext>());
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