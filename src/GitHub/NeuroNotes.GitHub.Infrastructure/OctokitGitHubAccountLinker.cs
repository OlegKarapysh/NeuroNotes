namespace NeuroNotes.GitHub.Infrastructure;

internal sealed class OctokitGitHubAccountLinker(
    IGitHubClientFactory clientFactory,
    IOptions<GitHubOptions> options) : IGitHubAccountLinker
{
    public async Task<Result<GitHubRepositorySettings>> Link(
        string repoInput,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (!GitHubRepositoryReference.TryParse(repoInput, out var owner, out var repo))
        {
            return new Error("That doesn't look like a GitHub repository. Send a URL like https://github.com/owner/repo.");
        }

        var client = clientFactory.Create(accessToken);

        try
        {
            var repository = await client.Repository.Get(owner, repo);

            if (repository.Permissions is { Push: false })
            {
                return new Error("The token can read this repository but cannot write to it. Use a token with Contents: write access.");
            }

            var branch = string.IsNullOrWhiteSpace(repository.DefaultBranch)
                ? options.Value.DefaultBranch
                : repository.DefaultBranch;

            return new GitHubRepositorySettings(
                repository.Owner.Login,
                repository.Name,
                branch,
                options.Value.NotesFolder,
                accessToken);
        }
        catch (AuthorizationException)
        {
            return new Error("GitHub rejected the access token. Check that it is valid and has Contents: write access.");
        }
        catch (NotFoundException)
        {
            return new Error($"Couldn't find {owner}/{repo}, or the token can't access it.");
        }
        catch (ApiException ex)
        {
            return new Error($"GitHub returned an error: {ex.Message}");
        }
    }
}