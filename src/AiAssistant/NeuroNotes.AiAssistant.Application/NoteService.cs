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
            - up to 7 keywords from the text, under a `keywords:` field (do NOT use a `tags:` field).
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

    public async Task<Result<CreatedNote>> GenerateNote(long botId, long userId, string text, CancellationToken cancellationToken = default)
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

        var tags = await ResolveTags(botId, userId, noteText, cancellationToken);
        var markdown = InjectTagsIntoFrontMatter(noteText, tags);
        var fileName = $"note_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md";

        return new CreatedNote(fileName, markdown, tags);
    }

    public Task SaveNote(long botId, long userId, CreatedNote note, CancellationToken cancellationToken = default) =>
        noteStore.SaveAsync(botId, userId, note.FileName, note.Markdown, note.Tags, cancellationToken);

    /// <summary>
    /// Picks, from the user's existing tags, the ones that fit the note. Best-effort: a missing tag list or an
    /// LLM/parse error yields no tags rather than failing note creation. Cancellation is never swallowed.
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveTags(long botId, long userId, string noteText, CancellationToken cancellationToken)
    {
        try
        {
            var availableTags = await tagStore.GetAllAsync(botId, userId, cancellationToken);
            if (availableTags.Count == 0)
            {
                return [];
            }

            var result = await tagSuggester.SuggestTags(noteText, availableTags, cancellationToken);
            return result.IsSuccess ? result.Value : [];
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Tagging is a nicety — never let it break note creation (but do honor cancellation).
            return [];
        }
    }

    /// <summary>
    /// Adds the tags to the note's YAML front matter under a single <c>tags:</c> key: merges into an existing
    /// <c>tags:</c> key if present (so the note never ends up with a duplicate key), otherwise appends a new
    /// <c>tags:</c> block. Front matter is only recognized when the note opens with a line that is exactly
    /// <c>---</c> and is closed by a later <c>---</c>/<c>...</c> line; otherwise a fresh front-matter block is
    /// prepended. Returns the markdown unchanged when there are no tags.
    /// </summary>
    public static string InjectTagsIntoFrontMatter(string markdown, IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return markdown;
        }

        var lines = new List<string>(markdown.Split('\n'));

        if (!TryFindFrontMatterEnd(lines, out var closeIndex))
        {
            // No recognizable YAML front matter: prepend a fresh block.
            return $"---\n{BuildTagBlock(tags)}---\n\n{markdown}";
        }

        var tagsKeyIndex = FindTopLevelTagsKey(lines, closeIndex);
        if (tagsKeyIndex < 0)
        {
            lines.InsertRange(closeIndex, BuildTagBlockLines(tags));
        }
        else
        {
            MergeIntoExistingTagsKey(lines, tagsKeyIndex, closeIndex, tags);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Recognizes YAML front matter only when the first line is exactly <c>---</c> and a later line is exactly
    /// <c>---</c> or <c>...</c>. <paramref name="closeIndex"/> is the index of that closing delimiter.
    /// </summary>
    private static bool TryFindFrontMatterEnd(IReadOnlyList<string> lines, out int closeIndex)
    {
        closeIndex = -1;

        if (lines.Count == 0 || lines[0].Trim() != "---")
        {
            return false;
        }

        for (var i = 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed is "---" or "...")
            {
                closeIndex = i;
                return true;
            }
        }

        return false;
    }

    /// <summary>Finds a top-level (unindented) <c>tags:</c> key within the front matter, or -1 if there is none.</summary>
    private static int FindTopLevelTagsKey(IReadOnlyList<string> lines, int closeIndex)
    {
        for (var i = 1; i < closeIndex; i++)
        {
            var line = lines[i];
            if (line.Length > 0 && !char.IsWhiteSpace(line[0])
                && line.StartsWith("tags:", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static void MergeIntoExistingTagsKey(List<string> lines, int tagsKeyIndex, int closeIndex, IReadOnlyList<string> tags)
    {
        var value = lines[tagsKeyIndex]["tags:".Length..].Trim();

        // Inline flow sequence, e.g. `tags: [a, b]` — rewrite it with our tags merged in.
        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            var merged = MergeDistinct(ParseInlineList(value), tags);
            lines[tagsKeyIndex] = $"tags: [{string.Join(", ", merged.Select(YamlQuote))}]";
            return;
        }

        // Block sequence (or empty value) — append our tags as block items after any existing ones.
        var insertAt = tagsKeyIndex + 1;
        while (insertAt < closeIndex && IsBlockSequenceItem(lines[insertAt]))
        {
            insertAt++;
        }

        lines.InsertRange(insertAt, tags.Select(tag => $"  - {YamlQuote(tag)}"));
    }

    private static bool IsBlockSequenceItem(string line) =>
        line.Length > 0 && char.IsWhiteSpace(line[0]) && line.TrimStart().StartsWith('-');

    private static IReadOnlyList<string> ParseInlineList(string inlineSequence) =>
        inlineSequence[1..^1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Trim('"', '\''))
            .Where(item => item.Length > 0)
            .ToArray();

    private static IReadOnlyList<string> MergeDistinct(IReadOnlyList<string> existing, IReadOnlyList<string> additional)
    {
        var merged = new List<string>(existing);
        var seen = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var item in additional)
        {
            if (seen.Add(item))
            {
                merged.Add(item);
            }
        }

        return merged;
    }

    private static string BuildTagBlock(IReadOnlyList<string> tags) =>
        string.Join('\n', BuildTagBlockLines(tags)) + '\n';

    private static IEnumerable<string> BuildTagBlockLines(IReadOnlyList<string> tags)
    {
        yield return "tags:";
        foreach (var tag in tags)
        {
            yield return $"  - {YamlQuote(tag)}";
        }
    }

    private static string YamlQuote(string value) =>
        $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}