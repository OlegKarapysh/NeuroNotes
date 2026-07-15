using System.Text.Json;

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
        - Respond with a JSON object of the form {"tags": ["tag1", "tag2"]}.
        - If no tag fits, respond with {"tags": []}.
        - Return ONLY the JSON object. No explanations, no extra text.
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
        ResponseFormat = "json_object"
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

        return Result.Ok(ParseSelectedTags(response.Content, availableTags));
    }

    /// <summary>
    /// Parses the model's structured JSON tag selection (<c>{"tags": [...]}</c>) and keeps only tags that
    /// appear in <paramref name="availableTags"/> — matched case-insensitively and returned in their canonical
    /// casing and order, de-duplicated. Never invents tags; returns empty on malformed or empty input.
    /// </summary>
    public static IReadOnlyList<string> ParseSelectedTags(string? modelResponse, IReadOnlyList<string> availableTags)
    {
        if (string.IsNullOrWhiteSpace(modelResponse) || availableTags.Count == 0)
        {
            return [];
        }

        var selected = ExtractTagTokens(modelResponse);
        if (selected.Count == 0)
        {
            return [];
        }

        return availableTags
            .Where(selected.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static HashSet<string> ExtractTagTokens(string modelResponse)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(modelResponse);

            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("tags", out var tagsElement)
                && tagsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in tagsElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var value = element.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            tokens.Add(value.Trim());
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed JSON → no tags. (json_object mode should always yield valid JSON; this is a safety net.)
        }

        return tokens;
    }
}