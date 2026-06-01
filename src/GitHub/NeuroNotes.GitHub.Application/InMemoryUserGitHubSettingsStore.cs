namespace NeuroNotes.GitHub.Application;

/// <summary>
/// Per-user GitHub links held in memory for the process lifetime. Tokens are stored in plaintext
/// and lost on restart — the user re-links after a restart. Durable, encrypted storage is a roadmap item.
/// </summary>
public sealed class InMemoryUserGitHubSettingsStore : IUserGitHubSettingsStore
{
    private readonly ConcurrentDictionary<long, GitHubRepositorySettings> _store = new();

    public void Save(long userId, GitHubRepositorySettings settings) => _store[userId] = settings;

    public GitHubRepositorySettings? Get(long userId) => _store.GetValueOrDefault(userId);

    public void Remove(long userId) => _store.TryRemove(userId, out _);
}