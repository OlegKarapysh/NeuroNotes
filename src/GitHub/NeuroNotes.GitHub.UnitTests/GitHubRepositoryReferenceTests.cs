using NeuroNotes.GitHub.Application;

namespace NeuroNotes.GitHub.UnitTests;

public class GitHubRepositoryReferenceTests
{
    [Theory]
    [InlineData("https://github.com/octocat/hello-world", "octocat", "hello-world")]
    [InlineData("https://github.com/octocat/hello-world.git", "octocat", "hello-world")]
    [InlineData("https://github.com/octocat/hello-world/", "octocat", "hello-world")]
    [InlineData("http://github.com/octocat/hello-world", "octocat", "hello-world")]
    [InlineData("github.com/octocat/hello-world", "octocat", "hello-world")]
    [InlineData("git@github.com:octocat/hello-world.git", "octocat", "hello-world")]
    [InlineData("octocat/hello-world", "octocat", "hello-world")]
    [InlineData("  octocat/hello-world  ", "octocat", "hello-world")]
    [InlineData("https://github.com/octocat/hello-world/tree/main", "octocat", "hello-world")]
    public void TryParse_ValidInputs_ReturnsOwnerAndRepo(string input, string expectedOwner, string expectedRepo)
    {
        var parsed = GitHubRepositoryReference.TryParse(input, out var owner, out var repo);

        Assert.True(parsed);
        Assert.Equal(expectedOwner, owner);
        Assert.Equal(expectedRepo, repo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("octocat")]
    [InlineData("https://github.com/octocat")]
    public void TryParse_InvalidInputs_ReturnsFalse(string? input)
    {
        var parsed = GitHubRepositoryReference.TryParse(input, out var owner, out var repo);

        Assert.False(parsed);
        Assert.Equal(string.Empty, owner);
        Assert.Equal(string.Empty, repo);
    }
}