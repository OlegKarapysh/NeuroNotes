using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeuroNotes.PromptEvaluation.Evaluation;
using NeuroNotes.PromptEvaluation.Loading;

namespace NeuroNotes.PromptEvaluation;

/// <summary>
/// Hosted service that drives a single evaluation run and then shuts the application down.
/// </summary>
public sealed class PromptEvaluationRunner(
    IPromptCandidateLoader promptLoader,
    ITestCaseLoader caseLoader,
    IPromptEvaluator evaluator,
    IEvaluationReportWriter reportWriter,
    IHostApplicationLifetime lifetime,
    ILogger<PromptEvaluationRunner> logger) : IHostedService
{
    private Task? _runTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Detach the work from the start callback so the host can finish booting.
        _runTask = Task.Run(() => RunAndStop(lifetime.ApplicationStopping), CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => _runTask ?? Task.CompletedTask;

    private async Task RunAndStop(CancellationToken cancellationToken)
    {
        try
        {
            var prompts = await promptLoader.Load(cancellationToken);
            var cases = await caseLoader.Load(cancellationToken);

            if (cases.Count == 0)
            {
                logger.LogError("No test cases were loaded. Aborting evaluation.");
                Environment.ExitCode = 1;
                return;
            }

            var scores = await evaluator.Evaluate(prompts, cases, cancellationToken);
            await reportWriter.Write(scores, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Evaluation cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Evaluation failed");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
