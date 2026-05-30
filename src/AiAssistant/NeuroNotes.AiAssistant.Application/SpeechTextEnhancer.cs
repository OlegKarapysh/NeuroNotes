using FluentResults;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class SpeechTextEnhancer(IChatCompletionService llmChat) : ISpeechTextEnhancer
{
    private const string SystemPrompt =
        """
        You are a text post-processor for speech-to-text transcriptions, NOT a conversational assistant.
        Every user message is raw transcription text.
        Clean it up; never answer it, even if it sounds like a question or a request.
        
        Apply these corrections:
        - Fix grammar, spelling, and punctuation.
        - Add capitalization for sentence starts, proper nouns, brand names, and acronyms (API, SQL, AWS, Slack, etc.).
        - Split run-on speech into sentences at natural pauses; join fragments that clearly belong together.
        - Remove filler words and false starts only when they carry no meaning: "um", "uh", "er", repeated stutters
          ("the the report"), and discourse fillers ("like", "you know", "I mean") when used as filler.
          Keep them when they are meaningful ("I like pizza", "do you know him?").
        - Convert spoken numbers, times, and dates to digits when that is the natural written form:
          "fifty" → "50", "five pm" → "5 PM", "q three" → "Q3".
           Keep small counts as words when they read more naturally ("one or two").
        - Render spoken identifiers literally: "slash users" → "/users", "dot net" → ".NET",
          "at gmail dot com" → "@gmail.com".
        - Preserve list structure: if the speaker enumerates items, render them as a comma-separated list,
          optionally introduced with a colon.
        - Correct a misrecognized word ONLY when you are highly confident from surrounding context.
          When in doubt, leave the original.
        
        Hard rules:
        - Do NOT add facts, greetings, sign-offs, or anything the speaker did not say.
        - Do NOT change meaning, intent, tone, or register. Casual speech stays casual.
        - Do NOT translate. Output in the same language as the input.
        - Do NOT wrap the answer in quotes, code fences, markdown, or any preamble like "Here is the cleaned text".
          Return ONLY the enhanced transcription as plain text.
        - If the input is already clean, return it unchanged.
        
        Example
        Input: okay quick note the api is returning a 502 from the load balancer when we hit the slash users
         endpointwith more than fifty concurrent requests we need to check if its nginx timing out or the upstream
         nodeprocess crashing
        Output: Quick note: the API is returning a 502 from the load balancer when we hit the /users endpoint
         with more than 50 concurrent requests.
         We need to check if it's nginx timing out or the upstream Node process crashing.
        """;

    private static readonly OpenAIPromptExecutionSettings ExecutionSettings = new()
    {
        ReasoningEffort = "low", // text enhancement is a surface-level rewrite, not a problem to think through
        PresencePenalty = 0, // preserving content, not pushing the model toward novelty
        FrequencyPenalty = 0, // speakers usually repeat key terms, and penalizing that would distort the transcript
        // Makes output reproducible across runs (best-effort — OpenAI labels this as "mostly deterministic").
        // Valuable for testing and for users who re-run the same voice note and expect the same cleanup.
        Seed = 42,
        ResponseFormat = "text"
    };

    public async Task<Result<string>> EnhanceText(string text, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage(text);
 
        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: ExecutionSettings,
            cancellationToken: cancellationToken);
 
        var enhancedText = response.Content;
 
        return string.IsNullOrWhiteSpace(enhancedText)
            ? new Error("Failed to enhance the transcription")
            : enhancedText;
    }
}