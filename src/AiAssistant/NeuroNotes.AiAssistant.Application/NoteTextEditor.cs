using FluentResults;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class NoteTextEditor(IChatCompletionService llmChat) : INoteTextEditor
{
    private const string SystemPrompt =
        """
        You are a text editor. The user provides a piece of text and an instruction describing how to change it.
        Apply the instruction precisely and return ONLY the updated text — no explanations, no metadata, no surrounding formatting, no quotation marks.

        Rules:
        - Preserve the original language and overall tone unless the instruction explicitly says otherwise.
        - Do not invent facts that are not implied by the original text or the instruction.
        - If the instruction is unclear, prefer minimal changes.
        - Keep the text self-contained; do not address the user directly.
        """;

    private const string UserPromptTemplate =
        """
        --- CURRENT TEXT ---
        {0}
        --- END TEXT ---

        Instruction: {1}
        """;

    private static readonly OpenAIPromptExecutionSettings ExecutionSettings = new()
    {
        ReasoningEffort = "low",
        PresencePenalty = 0,
        FrequencyPenalty = 0,
        Seed = 42,
        ResponseFormat = "text"
    };

    public async Task<Result<string>> EditText(
        string currentText,
        string editPrompt,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage(string.Format(UserPromptTemplate, currentText, editPrompt));

        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: ExecutionSettings,
            cancellationToken: cancellationToken);

        var editedText = response.Content;

        return string.IsNullOrWhiteSpace(editedText)
            ? new Error("Failed to edit the transcription")
            : editedText;
    }
}