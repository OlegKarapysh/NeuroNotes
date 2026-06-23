var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMassTransit();

builder.Services.AddAudioProcessingModule();
builder.Services.AddTelegramBotModule(builder.Environment);
builder.Services.AddAiAssistantModule();
builder.Services.AddGitHubModule();
builder.Services.AddPersistenceModule();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// `dotnet NeuroNotes.WebApi.dll migrate` applies pending EF migrations and exits without
// starting the host. The deploy workflow runs this in a one-off container before each rollout.
if (args.Contains("migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<NeuroNotesDbContext>().Database.MigrateAsync();
    return;
}

// `dotnet NeuroNotes.WebApi.dll loadtest [opts]` drives the real voice pipeline at controlled
// concurrency to measure the droplet's capacity, then exits without starting the host.
// See LoadTest/README.md. Run it as a one-off container against prod (no Telegram/DB involvement).
if (args.Contains("loadtest"))
{
    await NeuroNotes.WebApi.LoadTest.LoadTestRunner.RunAsync(app.Services, args);
    return;
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTelegramEndpoints();

await app.RunAsync();