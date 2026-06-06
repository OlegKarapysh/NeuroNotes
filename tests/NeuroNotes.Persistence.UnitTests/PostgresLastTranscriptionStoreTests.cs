using NeuroNotes.Persistence.Infrastructure.Repositories;

namespace NeuroNotes.Persistence.UnitTests;

public class PostgresLastTranscriptionStoreTests
{
    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNothingStored()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        Assert.Null(await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsTranscription()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        await store.SaveAsync(chatId: 1, "hello world", TestContext.Current.CancellationToken);

        Assert.Equal("hello world", await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_Overwrites_ExistingTranscription()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);
        await store.SaveAsync(chatId: 1, "first", TestContext.Current.CancellationToken);

        await store.SaveAsync(chatId: 1, "second", TestContext.Current.CancellationToken);

        Assert.Equal("second", await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_KeepsTranscription_SeparatePerChat()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresLastTranscriptionStore(dbContext);

        await store.SaveAsync(chatId: 1, "first", TestContext.Current.CancellationToken);
        await store.SaveAsync(chatId: 2, "second", TestContext.Current.CancellationToken);

        Assert.Equal("first", await store.GetAsync(chatId: 1, TestContext.Current.CancellationToken));
        Assert.Equal("second", await store.GetAsync(chatId: 2, TestContext.Current.CancellationToken));
    }
}