var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .ConfigureTelegramOptions(builder.Configuration)
    .AddTelegramBot();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", async ([FromServices] ITelegramBotClient client) => await client.GetMe());

await app.RunAsync();