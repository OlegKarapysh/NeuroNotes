using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Loading;

public interface IPromptCandidateLoader
{
    Task<IReadOnlyList<PromptCandidate>> Load(CancellationToken cancellationToken = default);
}
