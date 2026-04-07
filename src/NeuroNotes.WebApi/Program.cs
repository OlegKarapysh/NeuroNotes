using CaseConverter;
using NeuroNotes.WebApi.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMassTransit(config =>
{
    config.SetKebabCaseEndpointNameFormatter();
    config.AddConsumers(typeof(Program).Assembly);
    EndpointConvention.Map<ProcessVoiceMessageCommand>(new Uri($"queue:{nameof(ProcessVoiceMessageCommandHandler).ToKebabCase()}"));
    config.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
});

builder.Services
    .ConfigureTelegramOptions()
    .AddTelegramBot(builder.Environment);

builder.Services
    .ConfigureAudioConversionOptions()
    .AddAudioConversion();

builder.Services
    .ConfigureSpeechRecognitionOptions()
    .AddWhisperServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTelegramEndpoints();

await app.RunAsync();