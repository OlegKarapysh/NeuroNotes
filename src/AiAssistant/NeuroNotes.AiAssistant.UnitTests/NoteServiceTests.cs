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
}