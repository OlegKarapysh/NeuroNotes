var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMassTransit();

builder.Services.AddAudioProcessingModule();

builder.Services.AddTelegramBotModule(builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTelegramEndpoints();

await app.RunAsync();