using FluentResults;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace NeuroNotes.PromptEvaluation.Enhancers;

public sealed class PromptedSpeechTextEnhancer(IChatCompletionService llmChat) : IPromptedSpeechTextEnhancer
{
    // Mirror the production execution settings so the evaluation reflects real behavior.
    private static readonly OpenAIPromptExecutionSettings ExecutionSettings = new()
    {
        ReasoningEffort = "low",
        PresencePenalty = 0,
        FrequencyPenalty = 0,
        Seed = 42,
        ResponseFormat = "text"
    };

    public async Task<Result<string>> Enhance(
        string rawTranscription,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(rawTranscription);

        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: ExecutionSettings,
            cancellationToken: cancellationToken);

        var enhancedText = response.Content;

        return string.IsNullOrWhiteSpace(enhancedText)
            ? new Error("Model returned an empty enhancement")
            : enhancedText;
    }
}
