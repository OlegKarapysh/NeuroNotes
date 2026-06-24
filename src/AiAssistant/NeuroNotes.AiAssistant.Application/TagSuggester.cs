namespace NeuroNotes.AiAssistant.Application;

public sealed class TagSuggester(IChatCompletionService llmChat) : ITagSuggester
{
    private const string SystemPrompt =
        """
        You help organize notes by tagging them.
        You are given a note and a fixed list of allowed tags.
        Choose the tags from the allowed list that best match the note's content.

        Rules:
        - Pick ONLY tags that appear in the allowed list. Never invent new tags.
        - Pick only tags that genuinely fit the note. It is fine to pick none.
        - Return the chosen tags as a single comma-separated line, e.g. "work, ideas".
        - If no tag fits, return exactly: NONE
        - Return ONLY the tags (or NONE). No explanations, no extra text.
        """;

    private const string UserPromptTemplate =
        """
        --- ALLOWED TAGS ---
        {0}
        --- END ALLOWED TAGS ---

        --- NOTE ---
        {1}
        --- END NOTE ---
        """;

    private static readonly OpenAIPromptExecutionSettings ExecutionSettings = new()
    {
        Seed = 42,
        ResponseFormat = "text"
    };

    public async Task<Result<IReadOnlyList<string>>> SuggestTags(
        string noteText,
        IReadOnlyList<string> availableTags,
        CancellationToken cancellationToken = default)
    {
        if (availableTags.Count == 0)
        {
            return Result.Ok<IReadOnlyList<string>>([]);
        }

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage(string.Format(
            UserPromptTemplate,
            string.Join(", ", availableTags),
            noteText));

        var response = await llmChat.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: ExecutionSettings,
            cancellationToken: cancellationToken);

        return Result.Ok(FilterToAvailableTags(response.Content, availableTags));
    }

    public static IReadOnlyList<string> FilterToAvailableTags(string? modelResponse, IReadOnlyList<string> availableTags)
    {
        if (string.IsNullOrWhiteSpace(modelResponse) || availableTags.Count == 0)
        {
            return [];
        }

        var mentioned = modelResponse
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim('#', '-', '•', '"', '\'', ' ', '.'))
            .Where(token => token.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return availableTags
            .Where(mentioned.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}