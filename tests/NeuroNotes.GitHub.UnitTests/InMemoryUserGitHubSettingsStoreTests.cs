using NeuroNotes.GitHub.Application;
using NeuroNotes.GitHub.Public;

namespace NeuroNotes.GitHub.UnitTests;

public class InMemoryUserGitHubSettingsStoreTests
{
    private static GitHubRepositorySettings Sample(string repo = "notes") =>
        new(Owner: "octocat", Repo: repo, Branch: "main", NotesFolder: "notes", AccessToken: "token");

    [Fact]
    public void Get_ReturnsNull_WhenNothingSaved()
    {
        var store = new InMemoryUserGitHubSettingsStore();

        Assert.Null(store.Get(userId: 1));
    }

    [Fact]
    public void Save_ThenGet_ReturnsSettings()
    {
        var store = new InMemoryUserGitHubSettingsStore();
        var settings = Sample();

        store.Save(userId: 1, settings);

        Assert.Equal(settings, store.Get(userId: 1));
    }

    [Fact]
    public void Save_Overwrites_ExistingSettings()
    {
        var store = new InMemoryUserGitHubSettingsStore();

        store.Save(userId: 1, Sample("first"));
        store.Save(userId: 1, Sample("second"));

        Assert.Equal("second", store.Get(userId: 1)!.Repo);
    }

    [Fact]
    public void Settings_AreIsolated_PerUser()
    {
        var store = new InMemoryUserGitHubSettingsStore();

        store.Save(userId: 1, Sample("one"));
        store.Save(userId: 2, Sample("two"));

        Assert.Equal("one", store.Get(userId: 1)!.Repo);
        Assert.Equal("two", store.Get(userId: 2)!.Repo);
    }

    [Fact]
    public void Remove_DeletesSettings()
    {
        var store = new InMemoryUserGitHubSettingsStore();
        store.Save(userId: 1, Sample());

        store.Remove(userId: 1);

        Assert.Null(store.Get(userId: 1));
    }
}