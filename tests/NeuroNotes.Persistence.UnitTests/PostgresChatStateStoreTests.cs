using NeuroNotes.Persistence.Infrastructure.Repositories;
using NeuroNotes.TelegramBot.Public;

namespace NeuroNotes.Persistence.UnitTests;

public class PostgresChatStateStoreTests
{
    [Fact]
    public async Task GetAsync_ReturnsInitial_WhenNothingStored()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        Assert.Equal(ChatState.Initial, await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsState()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        await store.SetAsync(chatId: 1, ChatState.AwaitingGitHubToken, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.AwaitingGitHubToken, await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_Overwrites_ExistingState()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);
        await store.SetAsync(chatId: 1, ChatState.AwaitingTagName, TestContext.Current.CancellationToken);

        await store.SetAsync(chatId: 1, ChatState.HasTranscription, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.HasTranscription, await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_KeepsState_SeparatePerChat()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        await store.SetAsync(chatId: 1, ChatState.AwaitingEditPrompt, TestContext.Current.CancellationToken);
        await store.SetAsync(chatId: 2, ChatState.HasTranscription, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.AwaitingEditPrompt, await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
        Assert.Equal(ChatState.HasTranscription, await store.GetAsync(chatId: 2, TestContext.Current.CancellationToken));
    }
}