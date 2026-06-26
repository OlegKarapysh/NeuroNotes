namespace NeuroNotes.AiAssistant.Infrastructure.Configurations;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAiAssistantModule()
        {
            services.ConfigureAiAssistantOptions();

            services.AddSemanticKernel();

            services.AddApplicationServices();

            services.AddAiAssistantPersistence();

            return services;
        }

        public IServiceCollection ConfigureAiAssistantOptions()
        {
            services.AddOptions<AiAssistantOptions>()
                .BindConfiguration(AiAssistantOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public IServiceCollection AddSemanticKernel()
        {
            services.AddKernel();
            services.AddSingleton<IChatCompletionService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AiAssistantOptions>>().Value;
                return new OpenAIChatCompletionService(
                    modelId: options.DefaultModelId,
                    apiKey: options.OpenAiApiKey);
            });

            return services;
        }

        public IServiceCollection AddApplicationServices()
        {
            services.AddScoped<ISpeechTextEnhancer, SpeechTextEnhancer>();
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<INoteAssistant, NoteAssistant>();
            services.AddScoped<INoteTextEditor, NoteTextEditor>();
            services.AddScoped<ITagSuggester, TagSuggester>();

            return services;
        }
    }
}