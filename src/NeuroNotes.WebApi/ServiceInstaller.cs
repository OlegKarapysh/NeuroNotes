using NeuroNotes.TelegramBot.Application.Commands;

namespace NeuroNotes.WebApi;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMassTransit()
        {
            return services.AddMassTransit(config =>
            {
                config.SetKebabCaseEndpointNameFormatter();

                config.AddConsumers(
                    type => type != typeof(ProcessVoiceMessageCommandHandler),
                    typeof(AssemblyMarker).Assembly);
                config.AddConsumer<ProcessVoiceMessageCommandHandler, ProcessVoiceMessageCommandHandlerDefinition>();

                config.MapTelegramCommandEndpoints();
                config.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
            });
        }
    }
}