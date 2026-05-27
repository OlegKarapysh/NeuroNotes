using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Loading;

public interface ITestCaseLoader
{
    Task<IReadOnlyList<TranscriptionTestCase>> Load(CancellationToken cancellationToken = default);
}
