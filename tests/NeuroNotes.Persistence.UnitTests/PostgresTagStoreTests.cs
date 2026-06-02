using NeuroNotes.Persistence.Infrastructure.Repositories;

namespace NeuroNotes.Persistence.UnitTests;

public class PostgresTagStoreTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoTagsAdded()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresTagStore(dbContext);

        Assert.Empty(await store.GetAllAsync(userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_ReturnsTag()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresTagStore(dbContext);

        var result = await store.AddAsync(userId: 1, "work", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(["work"], await store.GetAllAsync(userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddAsync_Fails_WhenTagAlreadyExists()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresTagStore(dbContext);
        await store.AddAsync(userId: 1, "work", TestContext.Current.CancellationToken);

        var result = await store.AddAsync(userId: 1, "work", TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
        Assert.Equal("Tag \"work\" already exists.", result.Errors.First().Message);
    }

    [Fact]
    public async Task AddAsync_Fails_WhenTagDiffersOnlyByCase()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresTagStore(dbContext);
        await store.AddAsync(userId: 1, "Work", TestContext.Current.CancellationToken);

        var result = await store.AddAsync(userId: 1, "work", TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task AddAsync_KeepsTags_SeparatePerUser()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresTagStore(dbContext);
        await store.AddAsync(userId: 1, "work", TestContext.Current.CancellationToken);

        var result = await store.AddAsync(userId: 2, "work", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(["work"], await store.GetAllAsync(userId: 2, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAllAsync_PreservesOriginalCasing()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresTagStore(dbContext);

        await store.AddAsync(userId: 1, "Deep Work", TestContext.Current.CancellationToken);

        Assert.Equal(["Deep Work"], await store.GetAllAsync(userId: 1, TestContext.Current.CancellationToken));
    }
}