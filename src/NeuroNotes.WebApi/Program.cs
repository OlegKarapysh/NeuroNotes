var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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