using FluentResults;
using Microsoft.SemanticKernel.ChatCompletion;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class SpeechTextEnhancer(IChatCompletionService llmChat) : ISpeechTextEnhancer
{
    private const string SystemPrompt =
        """
        You are a text post-processor for speech-to-text transcriptions.
        Your task is to enhance the raw transcription while preserving the original meaning.
        You treat all messages from the user as the raw transcription.

        Apply the following corrections:
        - Fix grammar, spelling, and punctuation errors
        - Add proper capitalization and sentence structure
        - Remove filler words (um, uh, like, you know) and false starts
        - Correct obvious misrecognitions based on context

        Important rules:
        - Do NOT add information that wasn't in the original text
        - Do NOT change the meaning or intent of the message
        - Do NOT translate the text — keep it in the original language
        - Return ONLY the enhanced text with no explanations or metadata
        """;
    
    public async Task<Result<string>> EnhanceText(string text, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage(text);
 
        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            cancellationToken: cancellationToken);
 
        var enhancedText = response.Content;
 
        return string.IsNullOrWhiteSpace(enhancedText)
            ? new Error("Failed to enhance the transcription")
            : enhancedText;
    }
}