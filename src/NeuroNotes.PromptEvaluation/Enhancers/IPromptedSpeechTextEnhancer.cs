using FluentResults;

namespace NeuroNotes.PromptEvaluation.Enhancers;

/// <summary>
/// Same role as <c>ISpeechTextEnhancer</c>, but the system prompt is supplied per call
/// so we can A/B different prompts in the evaluation pipeline.
/// </summary>
public interface IPromptedSpeechTextEnhancer
{
    Task<Result<string>> Enhance(string rawTranscription, string systemPrompt, CancellationToken cancellationToken = default);
}
