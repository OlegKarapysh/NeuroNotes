using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Evaluation;

public interface IEvaluationReportWriter
{
    Task Write(IReadOnlyList<PromptScore> scores, CancellationToken cancellationToken = default);
}
