using NeuroNotes.AiAssistant.Application;

namespace NeuroNotes.AiAssistant.UnitTests;

public class TagSuggesterTests
{
    private static readonly string[] AvailableTags = ["work", "ideas", "health"];

    [Fact]
    public void FilterToAvailableTags_KeepsOnlyTags_FromTheAllowedList()
    {
        var result = TagSuggester.FilterToAvailableTags("work, travel, ideas", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void FilterToAvailableTags_NeverInventsNewTags()
    {
        var result = TagSuggester.FilterToAvailableTags("travel, cooking, finance", AvailableTags);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterToAvailableTags_MatchesCaseInsensitively_AndPreservesCanonicalCasing()
    {
        var result = TagSuggester.FilterToAvailableTags("WORK, Ideas", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void FilterToAvailableTags_PreservesAvailableTagOrder()
    {
        var result = TagSuggester.FilterToAvailableTags("ideas, work", AvailableTags);

        Assert.Equal(["work", "ideas"], result);
    }

    [Fact]
    public void FilterToAvailableTags_DeduplicatesRepeatedTags()
    {
        var result = TagSuggester.FilterToAvailableTags("work, work, WORK", AvailableTags);

        Assert.Equal(["work"], result);
    }

    [Fact]
    public void FilterToAvailableTags_HandlesNewlineSeparatedAndDecoratedTokens()
    {
        var result = TagSuggester.FilterToAvailableTags("- #work\n- #health", AvailableTags);

        Assert.Equal(["work", "health"], result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NONE")]
    public void FilterToAvailableTags_ReturnsEmpty_WhenNothingMatches(string? response)
    {
        var result = TagSuggester.FilterToAvailableTags(response, AvailableTags);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterToAvailableTags_ReturnsEmpty_WhenNoTagsAreAvailable()
    {
        var result = TagSuggester.FilterToAvailableTags("work, ideas", []);

        Assert.Empty(result);
    }
}