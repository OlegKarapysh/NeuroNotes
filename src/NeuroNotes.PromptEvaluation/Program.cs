using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeuroNotes.AiAssistant.Infrastructure.Configurations;
using NeuroNotes.AudioProcessing.Infrastructure;
using NeuroNotes.PromptEvaluation;
using NeuroNotes.PromptEvaluation.Configuration;
using NeuroNotes.PromptEvaluation.Enhancers;
using NeuroNotes.PromptEvaluation.Evaluation;
using NeuroNotes.PromptEvaluation.Loading;
using NeuroNotes.PromptEvaluation.Metrics;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Reuse production-grade audio + LLM wiring so the evaluation matches real behavior.
builder.Services.AddAudioProcessingModule();

// We only need OpenAI/SemanticKernel from the AI assistant module; skip the app-services.
builder.Services.ConfigureAiAssistantOptions();
builder.Services.AddSemanticKernel();

builder.Services.AddOptions<PromptEvaluationOptions>()
    .BindConfiguration(PromptEvaluationOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<ITextNormalizer, DefaultTextNormalizer>();
builder.Services.AddSingleton<IPromptedSpeechTextEnhancer, PromptedSpeechTextEnhancer>();
builder.Services.AddSingleton<ITestCaseLoader, DirectoryTestCaseLoader>();
builder.Services.AddSingleton<IPromptCandidateLoader, DirectoryPromptCandidateLoader>();
builder.Services.AddSingleton<IPromptEvaluator, PromptEvaluator>();
builder.Services.AddSingleton<IEvaluationReportWriter, ConsoleAndFileReportWriter>();

builder.Services.AddHostedService<PromptEvaluationRunner>();

await builder.Build().RunAsync();
