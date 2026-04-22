using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NeuroNotes.AiAssistant.Application;
using NeuroNotes.AiAssistant.Public.Interfaces;

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
            
            return services;
        }
    }
}