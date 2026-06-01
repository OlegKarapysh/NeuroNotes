namespace NeuroNotes.GitHub.Infrastructure;

/// <summary>
/// Creates an Octokit client authenticated with a specific user's token. Each user has their own
/// token, so clients are built per call rather than registered as a shared authenticated instance.
/// </summary>
public interface IGitHubClientFactory
{
    IGitHubClient Create(string accessToken);
}

internal sealed class OctokitGitHubClientFactory(IOptions<GitHubOptions> options) : IGitHubClientFactory
{
    public IGitHubClient Create(string accessToken) =>
        new GitHubClient(new ProductHeaderValue(options.Value.ProductHeader))
        {
            Credentials = new Credentials(accessToken)
        };
}