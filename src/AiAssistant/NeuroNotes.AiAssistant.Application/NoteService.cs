using System.Text;
using FluentResults;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.AiAssistant.Application;

public sealed class NoteService(IChatCompletionService llmChat, INoteStore noteStore) : INoteService
{
    private const string CreateNoteSystemPrompt =
        """
        You are an Obsidian note creator.
        Your task is to analyze the text provided by user and turn it into the Obsidian note in Markdown format.
        Here is the algorythm on how to complete this task:
        
        - Analyze the provided text and its meaning
        - Add the YAML front matter at the beginning of the note. It must contain:
            - the name of the note based on its content;
            - the today's date;
            - up to 7 keywords from the text.
        - Then add the original text as the content of the note.

        Important rules:
        - Do NOT change the original text, it is the content of the note.
        - Do NOT change the meaning or intent of the message.
        - Do NOT translate the text — keep it in the original language.
        - Return ONLY the text of the note. This text will be inserted into .md file.
        """;
    
    private static readonly OpenAIPromptExecutionSettings NoteCreationExecutionSettings = new()
    {
        Seed = 42,
        ResponseFormat = "text"
    };
    
    public async Task<Result<Stream>> CreateNote(long userId, string text, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(CreateNoteSystemPrompt);
        chatHistory.AddUserMessage(text);
 
        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: NoteCreationExecutionSettings,
            cancellationToken: cancellationToken);
 
        var noteText = response.Content;
 
        if (string.IsNullOrWhiteSpace(noteText))
        {
            return new Error("Failed to enhance the transcription");
        }

        var fileName = $"note_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md";
        noteStore.Save(userId, fileName, noteText);

        return await CreateMdFile(noteText, cancellationToken);
    }

    private async Task<Stream> CreateMdFile(string noteText, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync(noteText);
        await writer.FlushAsync(cancellationToken);
        
        return memoryStream;
    }
}