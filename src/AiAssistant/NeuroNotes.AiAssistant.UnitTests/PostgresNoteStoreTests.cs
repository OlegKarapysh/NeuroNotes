using NeuroNotes.AiAssistant.Persistence.Repositories;

namespace NeuroNotes.AiAssistant.UnitTests;

public class PostgresNoteStoreTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoNotesSaved()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresNoteStore(dbContext);

        Assert.Empty(await store.GetAllAsync(userId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_ThenGetAllAsync_ReturnsNote()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "note.md", "# Hello", TestContext.Current.CancellationToken);

        var notes = await store.GetAllAsync(userId: 1, TestContext.Current.CancellationToken);
        var note = Assert.Single(notes);
        Assert.Equal("note.md", note.FileName);
        Assert.Equal("# Hello", note.Content);
        Assert.NotEqual(default, note.SavedAt);
    }

    [Fact]
    public async Task GetAllAsync_KeepsNotes_SeparatePerUser()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "first.md", "first", TestContext.Current.CancellationToken);
        await store.SaveAsync(userId: 2, "second.md", "second", TestContext.Current.CancellationToken);

        var notes = await store.GetAllAsync(userId: 2, TestContext.Current.CancellationToken);
        Assert.Equal("second.md", Assert.Single(notes).FileName);
    }
}