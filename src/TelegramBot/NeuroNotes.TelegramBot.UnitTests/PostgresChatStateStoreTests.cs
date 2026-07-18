using NeuroNotes.TelegramBot.Persistence.Repositories;
using NeuroNotes.TelegramBot.Public;

namespace NeuroNotes.TelegramBot.UnitTests;

public class PostgresChatStateStoreTests
{
    [Fact]
    public async Task GetAsync_ReturnsInitial_WhenNothingStored()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        Assert.Equal(ChatState.Initial, await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsState()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        await store.SetAsync(botId: 1, chatId: 1, ChatState.AwaitingGitHubToken, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.AwaitingGitHubToken, await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_Overwrites_ExistingState()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);
        await store.SetAsync(botId: 1, chatId: 1, ChatState.AwaitingTagName, TestContext.Current.CancellationToken);

        await store.SetAsync(botId: 1, chatId: 1, ChatState.HasTranscription, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.HasTranscription, await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_KeepsState_SeparatePerChat()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        await store.SetAsync(botId: 1, chatId: 1, ChatState.AwaitingEditPrompt, TestContext.Current.CancellationToken);
        await store.SetAsync(botId: 1, chatId: 2, ChatState.HasTranscription, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.AwaitingEditPrompt, await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
        Assert.Equal(ChatState.HasTranscription, await store.GetAsync(botId: 1, chatId: 2, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetAsync_KeepsState_SeparatePerBot_EvenForTheSameChatId()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresChatStateStore(dbContext);

        await store.SetAsync(botId: 1, chatId: 1, ChatState.AwaitingEditPrompt, TestContext.Current.CancellationToken);
        await store.SetAsync(botId: 2, chatId: 1, ChatState.HasTranscription, TestContext.Current.CancellationToken);

        Assert.Equal(ChatState.AwaitingEditPrompt, await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
        Assert.Equal(ChatState.HasTranscription, await store.GetAsync(botId: 2, chatId: 1, TestContext.Current.CancellationToken));
    }
}