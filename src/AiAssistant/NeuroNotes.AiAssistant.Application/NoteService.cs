namespace NeuroNotes.AiAssistant.Application;

public sealed class NoteService(
    IChatCompletionService llmChat,
    INoteStore noteStore,
    ITagStore tagStore,
    ITagSuggester tagSuggester) : INoteService
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

    public async Task<Result<CreatedNote>> GenerateNote(long userId, string text, CancellationToken cancellationToken = default)
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

        var tags = await ResolveTags(userId, noteText, cancellationToken);
        var markdown = InjectTagsIntoFrontMatter(noteText, tags);
        var fileName = $"note_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md";

        return new CreatedNote(fileName, markdown, tags);
    }

    public Task SaveNote(long userId, CreatedNote note, CancellationToken cancellationToken = default) =>
        noteStore.SaveAsync(userId, note.FileName, note.Markdown, note.Tags, cancellationToken);

    /// <summary>
    /// Picks, from the user's existing tags, the ones that fit the note. Best-effort: any failure
    /// (no tags configured, LLM error, parse error) yields no tags rather than failing note creation.
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveTags(long userId, string noteText, CancellationToken cancellationToken)
    {
        try
        {
            var availableTags = await tagStore.GetAllAsync(userId, cancellationToken);
            if (availableTags.Count == 0)
            {
                return [];
            }

            var result = await tagSuggester.SuggestTags(noteText, availableTags, cancellationToken);
            return result.IsSuccess ? result.Value : [];
        }
        catch (Exception)
        {
            // Tagging is a nicety — never let it break note creation.
            return [];
        }
    }

    /// <summary>
    /// Inserts a <c>tags:</c> block into the note's YAML front matter, or prepends a front-matter block if
    /// the note has none. Returns the markdown unchanged when there are no tags.
    /// </summary>
    public static string InjectTagsIntoFrontMatter(string markdown, IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return markdown;
        }

        var tagBlock = BuildTagBlock(tags);

        // If the note opens with a YAML front matter block, insert the tags before its closing '---'.
        if (markdown.StartsWith("---", StringComparison.Ordinal))
        {
            var firstLineEnd = markdown.IndexOf('\n');
            if (firstLineEnd >= 0)
            {
                var closingDelimiter = markdown.IndexOf("\n---", firstLineEnd, StringComparison.Ordinal);
                if (closingDelimiter >= 0)
                {
                    var insertAt = closingDelimiter + 1;
                    return markdown[..insertAt] + tagBlock + markdown[insertAt..];
                }
            }
        }

        return $"---\n{tagBlock}---\n\n{markdown}";
    }

    private static string BuildTagBlock(IReadOnlyList<string> tags)
    {
        var builder = new StringBuilder("tags:\n");
        foreach (var tag in tags)
        {
            builder.Append("  - ").Append(YamlQuote(tag)).Append('\n');
        }

        return builder.ToString();
    }

    private static string YamlQuote(string value) =>
        $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}