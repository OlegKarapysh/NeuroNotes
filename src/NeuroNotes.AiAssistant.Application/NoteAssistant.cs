using System.Text;
using FluentResults;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class NoteAssistant(IChatCompletionService llmChat, INoteStore noteStore) : INoteAssistant
{
    private const string SystemPromptTemplate =
        """
        You are a personal note assistant.
        You help the user organize, recall, and reason about their personal notes.
        You have access to all of the user's notes, provided below in Markdown format.

        Rules:
        - Ground your answers in the user's notes whenever possible.
        - If the answer is not in the notes, say so explicitly instead of inventing facts.
        - Be concise and conversational. Reply in the same language as the user's message.
        - Do not expose raw YAML front matter or internal metadata unless the user asks.

        --- USER NOTES START ---
        {0}
        --- USER NOTES END ---
        """;

    private static readonly OpenAIPromptExecutionSettings ExecutionSettings = new()
    {
        ResponseFormat = "text"
    };

    public async Task<Result<string>> Ask(long userId, string question, CancellationToken cancellationToken = default)
    {
        var notes = noteStore.GetAll(userId);
        var systemPrompt = string.Format(SystemPromptTemplate, FormatNotes(notes));

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(question);

        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: ExecutionSettings,
            cancellationToken: cancellationToken);

        var answer = response.Content;

        return string.IsNullOrWhiteSpace(answer)
            ? new Error("Failed to get an answer from the assistant")
            : answer;
    }

    private static string FormatNotes(IReadOnlyList<StoredNote> notes)
    {
        if (notes.Count == 0)
        {
            return "(the user has no notes yet)";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < notes.Count; i++)
        {
            var note = notes[i];
            builder.AppendLine($"### Note {i + 1}: {note.FileName} (saved {note.SavedAt:u})");
            builder.AppendLine(note.Content);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
