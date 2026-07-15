using NeuroNotes.AiAssistant.Application;

namespace NeuroNotes.AiAssistant.UnitTests;

public class NoteServiceTests
{
    [Fact]
    public void InjectTagsIntoFrontMatter_ReturnsMarkdownUnchanged_WhenNoTags()
    {
        const string markdown = "---\ntitle: Test\n---\nbody";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, []);

        Assert.Equal(markdown, result);
    }

    [Fact]
    public void InjectTagsIntoFrontMatter_InsertsTagsBlock_IntoExistingFrontMatter()
    {
        const string markdown = "---\ntitle: Test\ndate: 2026-07-15\n---\nThe body.";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, ["work", "ideas"]);

        Assert.Equal(
            "---\ntitle: Test\ndate: 2026-07-15\ntags:\n  - \"work\"\n  - \"ideas\"\n---\nThe body.",
            result);
    }

    [Fact]
    public void InjectTagsIntoFrontMatter_PrependsFrontMatter_WhenNoneExists()
    {
        const string markdown = "Just a body, no front matter.";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, ["work"]);

        Assert.Equal("---\ntags:\n  - \"work\"\n---\n\nJust a body, no front matter.", result);
    }

    [Fact]
    public void InjectTagsIntoFrontMatter_QuotesMultiWordTags()
    {
        const string markdown = "---\ntitle: T\n---\nbody";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, ["Deep Work"]);

        Assert.Contains("  - \"Deep Work\"\n", result);
    }

    [Fact]
    public void InjectTagsIntoFrontMatter_MergesIntoExistingBlockTags_WithoutDuplicateKey()
    {
        const string markdown = "---\ntitle: T\ntags:\n  - keyword1\n---\nbody";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, ["work"]);

        Assert.Equal("---\ntitle: T\ntags:\n  - keyword1\n  - \"work\"\n---\nbody", result);
        Assert.Equal(1, CountOccurrences(result, "tags:"));
    }

    [Fact]
    public void InjectTagsIntoFrontMatter_MergesIntoExistingInlineTags_AndDeduplicates()
    {
        const string markdown = "---\ntags: [alpha, beta]\n---\nbody";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, ["beta", "work"]);

        Assert.Equal("---\ntags: [\"alpha\", \"beta\", \"work\"]\n---\nbody", result);
        Assert.Equal(1, CountOccurrences(result, "tags:"));
    }

    [Fact]
    public void InjectTagsIntoFrontMatter_PrependsBlock_WhenOpeningDelimiterHasNoClose()
    {
        // A leading '---' with no closing delimiter is not front matter — it must not be spliced into.
        const string markdown = "---\nnot front matter, no closing delimiter\nmore body";

        var result = NoteService.InjectTagsIntoFrontMatter(markdown, ["work"]);

        Assert.StartsWith("---\ntags:\n  - \"work\"\n---\n\n", result);
        Assert.EndsWith(markdown, result);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}