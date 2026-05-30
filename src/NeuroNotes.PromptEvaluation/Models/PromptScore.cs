namespace NeuroNotes.PromptEvaluation.Models;

public sealed record PromptScore(
    string PromptName,
    int CasesEvaluated,
    int Failures,
    decimal AverageDistance,
    decimal AverageSimilarity,
    IReadOnlyList<CaseEvaluationResult> Results);
