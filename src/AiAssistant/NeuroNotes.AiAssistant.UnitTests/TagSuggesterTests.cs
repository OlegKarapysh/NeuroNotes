using NeuroNotes.AiAssistant.Application;

namespace NeuroNotes.AiAssistant.UnitTests;

public class TagSuggesterTests
{
    private static readonly string[] AvailableTags = ["work", "ideas", "health"];

    [Fact]
    public void ParseSelectedTags_KeepsOnlyTags_FromTheAllowedList()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["work", "travel", "ideas"]}""", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void ParseSelectedTags_NeverInventsNewTags()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["travel", "cooking", "finance"]}""", AvailableTags);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseSelectedTags_MatchesCaseInsensitively_AndPreservesCanonicalCasing()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["WORK", "Ideas"]}""", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void ParseSelectedTags_PreservesAvailableTagOrder()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["ideas", "work"]}""", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void ParseSelectedTags_DeduplicatesRepeatedTags()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["work", "work", "WORK"]}""", AvailableTags);

        Assert.Equal(["work"], result);
    }

    [Fact]
    public void ParseSelectedTags_IgnoresNonStringArrayElements()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["work", 5, null, "ideas"]}""", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void ParseSelectedTags_ReturnsEmpty_WhenTagArrayIsEmpty()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": []}""", AvailableTags);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData("{\"tags\": \"work\"}")]
    [InlineData("{\"other\": [\"work\"]}")]
    public void ParseSelectedTags_ReturnsEmpty_WhenNothingUsableMatches(string? response)
    {
        var result = TagSuggester.ParseSelectedTags(response, AvailableTags);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseSelectedTags_ReturnsEmpty_WhenNoTagsAreAvailable()
    {
        var result = TagSuggester.ParseSelectedTags("""{"tags": ["work", "ideas"]}""", []);

        Assert.Empty(result);
    }
}