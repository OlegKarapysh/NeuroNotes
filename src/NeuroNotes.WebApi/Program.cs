var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMassTransit();

builder.Services.AddAudioProcessingModule();
builder.Services.AddTelegramBotModule(builder.Environment);
builder.Services.AddAiAssistantModule();
builder.Services.AddGitHubModule();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// `dotnet NeuroNotes.WebApi.dll migrate` applies pending EF migrations and exits without
// starting the host. The deploy workflow runs this in a one-off container before each rollout.
// Each module owns its own DbContext (registered as a base DbContext too), so migrate every one.
if (args.Contains("migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    foreach (var dbContext in scope.ServiceProvider.GetServices<DbContext>())
    {
        await dbContext.Database.MigrateAsync();
    }

    return;
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTelegramEndpoints();

await app.RunAsync();