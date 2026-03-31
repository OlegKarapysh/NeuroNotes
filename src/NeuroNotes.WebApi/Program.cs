using NeuroNotes.WebApi.Audio;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<AudioConversionOptions>(
    builder.Configuration.GetSection(AudioConversionOptions.SectionName));
builder.Services.AddSingleton<IAudioConverter, FFmpegAudioConverter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Server is running");

await app.RunAsync();