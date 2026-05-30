using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Evaluation;

public interface IPromptEvaluator
{
    Task<IReadOnlyList<PromptScore>> Evaluate(
        IReadOnlyList<PromptCandidate> prompts,
        IReadOnlyList<TranscriptionTestCase> cases,
        CancellationToken cancellationToken = default);
}
