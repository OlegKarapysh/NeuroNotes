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
                config.AddConsumers(typeof(NeuroNotes.TelegramBot.Application.AssemblyMarker).Assembly);
                config.MapTelegramCommandEndpoints();
                config.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
            });
        }
    }
}