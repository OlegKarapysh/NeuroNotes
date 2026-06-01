using NeuroNotes.TelegramBot.Application.Services;

namespace NeuroNotes.TelegramBot.UnitTests.Services;

public class PendingGitHubLinkStoreTests
{
    [Fact]
    public void GetRepo_ReturnsNull_WhenNothingPending()
    {
        var store = new PendingGitHubLinkStore();

        Assert.Null(store.GetRepo(chatId: 1));
    }

    [Fact]
    public void SetRepo_ThenGetRepo_ReturnsValue()
    {
        var store = new PendingGitHubLinkStore();

        store.SetRepo(chatId: 1, "owner/repo");

        Assert.Equal("owner/repo", store.GetRepo(chatId: 1));
    }

    [Fact]
    public void Clear_RemovesPendingRepo()
    {
        var store = new PendingGitHubLinkStore();
        store.SetRepo(chatId: 1, "owner/repo");

        store.Clear(chatId: 1);

        Assert.Null(store.GetRepo(chatId: 1));
    }
}