using NeuroNotes.GitHub.Persistence.Repositories;
using NeuroNotes.GitHub.Public;

namespace NeuroNotes.GitHub.UnitTests;

public class PostgresUserGitHubSettingsStoreTests
{
    private static readonly GitHubRepositorySettings Settings =
        new("octocat", "notes", "main", "notes", "token-1");

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNothingSaved()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);

        Assert.Null(await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsSettings()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);

        await store.SaveAsync(botId: 1, userId: 1, Settings, TestContext.Current.CancellationToken);

        Assert.Equal(Settings, await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_Overwrites_ExistingSettings()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);
        await store.SaveAsync(botId: 1, userId: 1, Settings, TestContext.Current.CancellationToken);

        var updated = Settings with { Repo = "other-repo", AccessToken = "token-2" };
        await store.SaveAsync(botId: 1, userId: 1, updated, TestContext.Current.CancellationToken);

        Assert.Equal(updated, await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_DeletesSettings()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);
        await store.SaveAsync(botId: 1, userId: 1, Settings, TestContext.Current.CancellationToken);

        await store.RemoveAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken);

        Assert.Null(await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_DoesNothing_WhenNothingSaved()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);

        await store.RemoveAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken);

        Assert.Null(await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_KeepsSettings_SeparatePerUser()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);
        var other = Settings with { Owner = "other-owner" };

        await store.SaveAsync(botId: 1, userId: 1, Settings, TestContext.Current.CancellationToken);
        await store.SaveAsync(botId: 1, userId: 2, other, TestContext.Current.CancellationToken);

        Assert.Equal(Settings, await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
        Assert.Equal(other, await store.GetAsync(botId: 1, userId: 2, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_KeepsSettings_SeparatePerBot_EvenForTheSameUserId()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);
        var other = Settings with { Owner = "other-owner", Repo = "other-repo" };

        await store.SaveAsync(botId: 1, userId: 1, Settings, TestContext.Current.CancellationToken);
        await store.SaveAsync(botId: 2, userId: 1, other, TestContext.Current.CancellationToken);

        Assert.Equal(Settings, await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
        Assert.Equal(other, await store.GetAsync(botId: 2, userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_OnlyAffectsTheGivenBot()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresUserGitHubSettingsStore(dbContext);
        var other = Settings with { Owner = "other-owner" };
        await store.SaveAsync(botId: 1, userId: 1, Settings, TestContext.Current.CancellationToken);
        await store.SaveAsync(botId: 2, userId: 1, other, TestContext.Current.CancellationToken);

        await store.RemoveAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken);

        Assert.Null(await store.GetAsync(botId: 1, userId: 1, TestContext.Current.CancellationToken));
        Assert.Equal(other, await store.GetAsync(botId: 2, userId: 1, TestContext.Current.CancellationToken));
    }
}