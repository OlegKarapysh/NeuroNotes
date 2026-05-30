namespace NeuroNotes.PromptEvaluation.Models;

/// <summary>
/// Result of evaluating a single (prompt, case) pair across one or more attempts.
/// The case-level score is the mean of the successful <see cref="Attempts"/>.
/// </summary>
public sealed record CaseEvaluationResult(
    string PromptName,
    string CaseName,
    string ReferenceText,
    IReadOnlyList<AttemptResult> Attempts,
    string? Error = null)
{
    /// <summary>
    /// Failed if a fatal error prevented attempts from running, or if every attempt errored.
    /// </summary>
    public bool IsFailure => Error is not null || Attempts.All(a => !a.IsSuccess);

    public decimal AverageDistance
    {
        get
        {
            var successes = Attempts.Where(a => a.IsSuccess).ToArray();
            return successes.Length == 0
                ? 0m
                : successes.Average(a => (decimal)a.LevenshteinDistance);
        }
    }

    public decimal AverageSimilarity
    {
        get
        {
            var successes = Attempts.Where(a => a.IsSuccess).ToArray();
            return successes.Length == 0
                ? 0m
                : successes.Average(a => a.Similarity);
        }
    }
}
