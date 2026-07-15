using Microsoft.EntityFrameworkCore;
using NeuroNotes.AiAssistant.Persistence.DbContexts;
using NeuroNotes.AiAssistant.Persistence.Entities;
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

        await store.SaveAsync(userId: 1, "note.md", "# Hello", [], TestContext.Current.CancellationToken);

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

        await store.SaveAsync(userId: 1, "first.md", "first", [], TestContext.Current.CancellationToken);
        await store.SaveAsync(userId: 2, "second.md", "second", [], TestContext.Current.CancellationToken);

        var notes = await store.GetAllAsync(userId: 2, TestContext.Current.CancellationToken);
        Assert.Equal("second.md", Assert.Single(notes).FileName);
    }

    [Fact]
    public async Task SaveAsync_WithTags_AssociatesNoteWithExistingTags()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        await SeedTagsAsync(dbContext, userId: 1, ("work", "WORK"), ("ideas", "IDEAS"));
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "note.md", "# Hi", ["work", "ideas"], TestContext.Current.CancellationToken);

        var note = Assert.Single(await dbContext.Notes.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        var associations = await dbContext.NoteTags.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, associations.Count);
        Assert.All(associations, association => Assert.Equal(note.Id, association.NoteId));
        var expectedTagIds = await dbContext.Tags.Where(tag => tag.UserId == 1).Select(tag => tag.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedTagIds.OrderBy(id => id), associations.Select(association => association.TagId).OrderBy(id => id));
    }

    [Fact]
    public async Task SaveAsync_MatchesTagNames_CaseInsensitively()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        await SeedTagsAsync(dbContext, userId: 1, ("work", "WORK"));
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "note.md", "# Hi", ["WORK"], TestContext.Current.CancellationToken);

        Assert.Single(await dbContext.NoteTags.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_IgnoresTagNames_ThatDoNotExistForTheUser()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        await SeedTagsAsync(dbContext, userId: 1, ("work", "WORK"));
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "note.md", "# Hi", ["work", "ghost"], TestContext.Current.CancellationToken);

        var association = Assert.Single(await dbContext.NoteTags.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        var workId = await dbContext.Tags.Where(tag => tag.UserId == 1 && tag.NormalizedName == "WORK")
            .Select(tag => tag.Id).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(workId, association.TagId);
    }

    [Fact]
    public async Task SaveAsync_DoesNotAssociateTags_OwnedByAnotherUser()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        await SeedTagsAsync(dbContext, userId: 2, ("work", "WORK"));
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "note.md", "# Hi", ["work"], TestContext.Current.CancellationToken);

        Assert.Empty(await dbContext.NoteTags.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_WithEmptyTags_CreatesNoAssociations()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var store = new PostgresNoteStore(dbContext);

        await store.SaveAsync(userId: 1, "note.md", "# Hi", [], TestContext.Current.CancellationToken);

        Assert.Empty(await dbContext.NoteTags.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    private static async Task SeedTagsAsync(
        AiAssistantDbContext dbContext, long userId, params (string Name, string NormalizedName)[] tags)
    {
        foreach (var (name, normalizedName) in tags)
        {
            dbContext.Tags.Add(new TagEntity { UserId = userId, Name = name, NormalizedName = normalizedName });
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}