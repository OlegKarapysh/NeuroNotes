namespace NeuroNotes.GitHub.Public.Interfaces;

public interface IUserGitHubSettingsStore
{
    Task SaveAsync(long userId, GitHubRepositorySettings settings, CancellationToken cancellationToken = default);
    Task<GitHubRepositorySettings?> GetAsync(long userId, CancellationToken cancellationToken = default);
    Task RemoveAsync(long userId, CancellationToken cancellationToken = default);
}