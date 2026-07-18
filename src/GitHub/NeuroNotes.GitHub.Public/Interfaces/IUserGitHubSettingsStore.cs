namespace NeuroNotes.GitHub.Public.Interfaces;

public interface IUserGitHubSettingsStore
{
    Task SaveAsync(long botId, long userId, GitHubRepositorySettings settings, CancellationToken cancellationToken = default);
    Task<GitHubRepositorySettings?> GetAsync(long botId, long userId, CancellationToken cancellationToken = default);
    Task RemoveAsync(long botId, long userId, CancellationToken cancellationToken = default);
}