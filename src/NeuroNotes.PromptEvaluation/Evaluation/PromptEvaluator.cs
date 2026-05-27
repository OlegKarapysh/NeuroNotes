using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.AudioProcessing.Public.Interfaces;
using NeuroNotes.PromptEvaluation.Configuration;
using NeuroNotes.PromptEvaluation.Enhancers;
using NeuroNotes.PromptEvaluation.Metrics;
using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Evaluation;

/// <summary>
/// For every (prompt, case) pair:
///   1. Transcribe the audio once via <see cref="IVoiceTranscriber"/> (raw Whisper output).
///   2. Run <c>AttemptsPerCase</c> enhancement attempts with the candidate prompt and score each.
///   3. The case score is the mean of the successful attempts' distance / similarity.
///
/// The per-prompt aggregate is then the mean of the case scores, so every case
/// contributes equally regardless of how many attempts succeeded for it.
///
/// Raw transcriptions are cached per case so each audio file is decoded only once,
/// regardless of how many prompts × attempts are being compared.
/// </summary>
public sealed class PromptEvaluator(
    IVoiceTranscriber voiceTranscriber,
    IPromptedSpeechTextEnhancer promptedEnhancer,
    ITextNormalizer textNormalizer,
    IOptions<PromptEvaluationOptions> options,
    ILogger<PromptEvaluator> logger) : IPromptEvaluator
{
    private readonly int _attemptsPerCase = options.Value.AttemptsPerCase;

    public async Task<IReadOnlyList<PromptScore>> Evaluate(
        IReadOnlyList<PromptCandidate> prompts,
        IReadOnlyList<TranscriptionTestCase> cases,
        CancellationToken cancellationToken = default)
    {
        if (prompts.Count == 0)
        {
            throw new ArgumentException("At least one prompt candidate is required", nameof(prompts));
        }

        if (cases.Count == 0)
        {
            throw new ArgumentException("At least one test case is required", nameof(cases));
        }

        if (_attemptsPerCase < 1)
        {
            throw new InvalidOperationException(
                $"{nameof(PromptEvaluationOptions.AttemptsPerCase)} must be >= 1 (got {_attemptsPerCase}).");
        }

        var rawTranscriptions = await TranscribeAllRaw(cases, cancellationToken);

        var scores = new List<PromptScore>(prompts.Count);

        foreach (var prompt in prompts)
        {
            logger.LogInformation(
                "Evaluating prompt '{Prompt}' ({Attempts} attempt(s) per case)",
                prompt.Name, _attemptsPerCase);

            var results = new List<CaseEvaluationResult>(cases.Count);
            foreach (var testCase in cases)
            {
                results.Add(await EvaluateSingle(
                    prompt,
                    testCase,
                    rawTranscriptions[testCase.Name],
                    cancellationToken));
            }

            scores.Add(Aggregate(prompt, results));
        }

        return scores
            .OrderByDescending(s => s.AverageSimilarity)
            .ThenBy(s => s.AverageDistance)
            .ToList();
    }

    private async Task<Dictionary<string, string?>> TranscribeAllRaw(
        IReadOnlyList<TranscriptionTestCase> cases,
        CancellationToken cancellationToken)
    {
        var raw = new Dictionary<string, string?>(cases.Count);

        foreach (var testCase in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("Transcribing raw audio for case '{Case}'", testCase.Name);

            try
            {
                await using var fileStream = File.OpenRead(testCase.AudioFilePath);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);

                var result = await voiceTranscriber.Transcribe(memoryStream);
                if (result.IsFailed)
                {
                    logger.LogError(
                        "Raw transcription failed for case '{Case}': {Error}",
                        testCase.Name, result.Errors.First().Message);
                    raw[testCase.Name] = null;
                }
                else
                {
                    raw[testCase.Name] = result.Value;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read or transcribe audio file '{Path}'", testCase.AudioFilePath);
                raw[testCase.Name] = null;
            }
        }

        return raw;
    }

    private async Task<CaseEvaluationResult> EvaluateSingle(
        PromptCandidate prompt,
        TranscriptionTestCase testCase,
        string? rawTranscription,
        CancellationToken cancellationToken)
    {
        if (rawTranscription is null)
        {
            return new CaseEvaluationResult(
                PromptName: prompt.Name,
                CaseName: testCase.Name,
                ReferenceText: testCase.ReferenceText,
                Attempts: [],
                Error: "Raw transcription failed");
        }

        var normalizedReference = textNormalizer.Normalize(testCase.ReferenceText);
        var attempts = new List<AttemptResult>(_attemptsPerCase);

        for (var attemptIndex = 1; attemptIndex <= _attemptsPerCase; attemptIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug(
                "Prompt '{Prompt}' case '{Case}' attempt {Index}/{Total}",
                prompt.Name, testCase.Name, attemptIndex, _attemptsPerCase);

            attempts.Add(await RunAttempt(
                attemptIndex,
                rawTranscription,
                normalizedReference,
                prompt.SystemPrompt,
                cancellationToken));
        }

        return new CaseEvaluationResult(
            PromptName: prompt.Name,
            CaseName: testCase.Name,
            ReferenceText: testCase.ReferenceText,
            Attempts: attempts);
    }

    private async Task<AttemptResult> RunAttempt(
        int attemptIndex,
        string rawTranscription,
        string normalizedReference,
        string systemPrompt,
        CancellationToken cancellationToken)
    {
        var enhanced = await promptedEnhancer.Enhance(rawTranscription, systemPrompt, cancellationToken);
        if (enhanced.IsFailed)
        {
            return new AttemptResult(
                AttemptIndex: attemptIndex,
                ProducedText: string.Empty,
                LevenshteinDistance: normalizedReference.Length,
                Similarity: 0m,
                Error: enhanced.Errors.First().Message);
        }

        var produced = enhanced.Value;
        var normalizedProduced = textNormalizer.Normalize(produced);

        var distance = LevenshteinDistance.Compute(normalizedReference, normalizedProduced);
        var similarity = LevenshteinDistance.Similarity(normalizedReference, normalizedProduced);

        return new AttemptResult(
            AttemptIndex: attemptIndex,
            ProducedText: produced,
            LevenshteinDistance: distance,
            Similarity: similarity);
    }

    private static PromptScore Aggregate(PromptCandidate prompt, IReadOnlyList<CaseEvaluationResult> results)
    {
        var successfulCases = results.Where(r => !r.IsFailure).ToList();

        var averageDistance = successfulCases.Count == 0
            ? 0m
            : successfulCases.Average(r => r.AverageDistance);

        var averageSimilarity = successfulCases.Count == 0
            ? 0m
            : successfulCases.Average(r => r.AverageSimilarity);

        return new PromptScore(
            PromptName: prompt.Name,
            CasesEvaluated: results.Count,
            Failures: results.Count - successfulCases.Count,
            AverageDistance: averageDistance,
            AverageSimilarity: averageSimilarity,
            Results: results);
    }
}
