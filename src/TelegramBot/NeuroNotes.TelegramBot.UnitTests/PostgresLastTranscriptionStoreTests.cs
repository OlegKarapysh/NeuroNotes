using NeuroNotes.TelegramBot.Persistence.Repositories;

namespace NeuroNotes.TelegramBot.UnitTests;

public class PostgresLastTranscriptionStoreTests
{
    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNothingStored()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        Assert.Null(await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsTranscription()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        await store.SaveAsync(botId: 1, chatId: 1, "hello world", TestContext.Current.CancellationToken);

        Assert.Equal("hello world", await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_Overwrites_ExistingTranscription()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);
        await store.SaveAsync(botId: 1, chatId: 1, "first", TestContext.Current.CancellationToken);

        await store.SaveAsync(botId: 1, chatId: 1, "second", TestContext.Current.CancellationToken);

        Assert.Equal("second", await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_KeepsTranscription_SeparatePerChat()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        await store.SaveAsync(botId: 1, chatId: 1, "first", TestContext.Current.CancellationToken);
        await store.SaveAsync(botId: 1, chatId: 2, "second", TestContext.Current.CancellationToken);

        Assert.Equal("first", await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
        Assert.Equal("second", await store.GetAsync(botId: 1, chatId: 2, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_KeepsTranscription_SeparatePerBot_EvenForTheSameChatId()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        await store.SaveAsync(botId: 1, chatId: 1, "bot one's transcription", TestContext.Current.CancellationToken);
        await store.SaveAsync(botId: 2, chatId: 1, "bot two's transcription", TestContext.Current.CancellationToken);

        Assert.Equal("bot one's transcription", await store.GetAsync(botId: 1, chatId: 1, TestContext.Current.CancellationToken));
        Assert.Equal("bot two's transcription", await store.GetAsync(botId: 2, chatId: 1, TestContext.Current.CancellationToken));
    }
}