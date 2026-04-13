using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
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
            
            services.AddScoped<ISpeechTextEnhancer, SpeechTextEnhancer>();
            
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
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<AiAssistantOptions>>();
            services.AddKernel()
                .AddOpenAIChatCompletion(
                    modelId: "gpt-4o-mini",
                    apiKey: options.Value.OpenAiApiKey);

            return services;
        }
    }
}