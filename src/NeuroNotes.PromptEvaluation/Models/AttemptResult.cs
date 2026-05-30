namespace NeuroNotes.PromptEvaluation.Models;

/// <summary>
/// One enhancement attempt against the same raw transcription with the same prompt.
/// Several of these are aggregated into a <see cref="CaseEvaluationResult"/> to smooth
/// out LLM sampling variance.
/// </summary>
public sealed record AttemptResult(
    int AttemptIndex,
    string ProducedText,
    int LevenshteinDistance,
    decimal Similarity,
    string? Error = null)
{
    public bool IsSuccess => Error is null;
}
