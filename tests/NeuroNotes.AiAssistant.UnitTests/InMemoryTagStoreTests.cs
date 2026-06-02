using NeuroNotes.AiAssistant.Application;

namespace NeuroNotes.AiAssistant.UnitTests;

public class InMemoryTagStoreTests
{
    [Fact]
    public void GetAll_ReturnsEmpty_WhenNoTagsAdded()
    {
        var store = new InMemoryTagStore();

        Assert.Empty(store.GetAll(userId: 1));
    }

    [Fact]
    public void Add_ThenGetAll_ReturnsTag()
    {
        var store = new InMemoryTagStore();

        var result = store.Add(userId: 1, "work");

        Assert.True(result.IsSuccess);
        Assert.Equal(["work"], store.GetAll(userId: 1));
    }

    [Fact]
    public void Add_Fails_WhenTagAlreadyExists()
    {
        var store = new InMemoryTagStore();
        store.Add(userId: 1, "work");

        var result = store.Add(userId: 1, "work");

        Assert.True(result.IsFailed);
        Assert.Equal("Tag \"work\" already exists.", result.Errors.First().Message);
    }

    [Fact]
    public void Add_Fails_WhenTagDiffersOnlyByCase()
    {
        var store = new InMemoryTagStore();
        store.Add(userId: 1, "Work");

        var result = store.Add(userId: 1, "work");

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Add_KeepsTags_SeparatePerUser()
    {
        var store = new InMemoryTagStore();
        store.Add(userId: 1, "work");

        var result = store.Add(userId: 2, "work");

        Assert.True(result.IsSuccess);
        Assert.Equal(["work"], store.GetAll(userId: 2));
    }
}