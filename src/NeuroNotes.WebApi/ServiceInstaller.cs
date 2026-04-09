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
                config.AddConsumers(typeof(Program).Assembly);
                config.MapTelegramCommandEndpoints();
                config.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
            });
        }
    }
}