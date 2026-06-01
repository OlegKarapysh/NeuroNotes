namespace NeuroNotes.GitHub.Public.Interfaces;

public interface IGitHubAccountLinker
{
    Task<Result<GitHubRepositorySettings>> Link(
        string repoInput,
        string accessToken,
        CancellationToken cancellationToken = default);
}