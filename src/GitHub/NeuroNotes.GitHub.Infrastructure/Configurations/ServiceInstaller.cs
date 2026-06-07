namespace NeuroNotes.GitHub.Infrastructure.Configurations;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGitHubModule()
        {
            services.ConfigureGitHubOptions();

            services.AddSingleton<IGitHubClientFactory, OctokitGitHubClientFactory>();
            services.AddScoped<IGitHubAccountLinker, OctokitGitHubAccountLinker>();
            services.AddScoped<IGitHubNotePublisher, OctokitGitHubNotePublisher>();

            services.AddGitHubPersistence();

            return services;
        }

        public IServiceCollection ConfigureGitHubOptions()
        {
            services.AddOptions<GitHubOptions>()
                .BindConfiguration(GitHubOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}