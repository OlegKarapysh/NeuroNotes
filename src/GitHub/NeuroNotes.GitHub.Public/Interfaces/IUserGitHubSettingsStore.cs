namespace NeuroNotes.GitHub.Public.Interfaces;

public interface IUserGitHubSettingsStore
{
    void Save(long userId, GitHubRepositorySettings settings);
    GitHubRepositorySettings? Get(long userId);
    void Remove(long userId);
}