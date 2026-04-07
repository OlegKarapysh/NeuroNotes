var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .ConfigureTelegramOptions()
    .AddTelegramBot(builder.Environment);

builder.Services
    .ConfigureAudioConversionOptions()
    .AddAudioConversion();

builder.Services.AddWhisperServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var whisperProcessorFactory = scope.ServiceProvider.GetRequiredService<IWhisperProcessorFactory>();
    await whisperProcessorFactory.Initialize();
});

app.MapTelegramEndpoints();

await app.RunAsync();