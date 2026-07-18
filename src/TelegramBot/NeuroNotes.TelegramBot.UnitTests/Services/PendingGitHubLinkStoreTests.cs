using NeuroNotes.TelegramBot.Application.Services;

namespace NeuroNotes.TelegramBot.UnitTests.Services;

public class PendingGitHubLinkStoreTests
{
    [Fact]
    public void GetRepo_ReturnsNull_WhenNothingPending()
    {
        var store = new PendingGitHubLinkStore();

        Assert.Null(store.GetRepo(botId: 1, chatId: 1));
    }

    [Fact]
    public void SetRepo_ThenGetRepo_ReturnsValue()
    {
        var store = new PendingGitHubLinkStore();

        store.SetRepo(botId: 1, chatId: 1, "owner/repo");

        Assert.Equal("owner/repo", store.GetRepo(botId: 1, chatId: 1));
    }

    [Fact]
    public void Clear_RemovesPendingRepo()
    {
        var store = new PendingGitHubLinkStore();
        store.SetRepo(botId: 1, chatId: 1, "owner/repo");

        store.Clear(botId: 1, chatId: 1);

        Assert.Null(store.GetRepo(botId: 1, chatId: 1));
    }

    [Fact]
    public void SetRepo_KeepsRepo_SeparatePerBot()
    {
        var store = new PendingGitHubLinkStore();

        store.SetRepo(botId: 1, chatId: 1, "owner/repo-a");
        store.SetRepo(botId: 2, chatId: 1, "owner/repo-b");

        Assert.Equal("owner/repo-a", store.GetRepo(botId: 1, chatId: 1));
        Assert.Equal("owner/repo-b", store.GetRepo(botId: 2, chatId: 1));
    }

    [Fact]
    public void Clear_OnlyAffectsTheGivenBot()
    {
        var store = new PendingGitHubLinkStore();
        store.SetRepo(botId: 1, chatId: 1, "owner/repo-a");
        store.SetRepo(botId: 2, chatId: 1, "owner/repo-b");

        store.Clear(botId: 1, chatId: 1);

        Assert.Null(store.GetRepo(botId: 1, chatId: 1));
        Assert.Equal("owner/repo-b", store.GetRepo(botId: 2, chatId: 1));
    }
}