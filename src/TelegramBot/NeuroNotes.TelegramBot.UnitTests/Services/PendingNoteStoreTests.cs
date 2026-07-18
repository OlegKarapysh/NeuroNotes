using NeuroNotes.AiAssistant.Public.Interfaces;
using NeuroNotes.TelegramBot.Application.Services;

namespace NeuroNotes.TelegramBot.UnitTests.Services;

public class PendingNoteStoreTests
{
    private static readonly CreatedNote SampleNote = new("note_20260714_120000.md", "---\ntitle: Test\n---\nbody", ["work"]);
    private static readonly CreatedNote OtherNote = new("note_20260714_130000.md", "---\ntitle: Other\n---\nbody", ["ideas"]);

    [Fact]
    public void Get_ReturnsNull_WhenNothingPending()
    {
        var store = new PendingNoteStore();

        Assert.Null(store.Get(botId: 1, chatId: 1));
    }

    [Fact]
    public void Set_ThenGet_ReturnsSameNote()
    {
        var store = new PendingNoteStore();

        store.Set(botId: 1, chatId: 1, SampleNote);

        Assert.Equal(SampleNote, store.Get(botId: 1, chatId: 1));
    }

    [Fact]
    public void Set_IsPerChat()
    {
        var store = new PendingNoteStore();

        store.Set(botId: 1, chatId: 1, SampleNote);

        Assert.Null(store.Get(botId: 1, chatId: 2));
    }

    [Fact]
    public void Clear_RemovesPendingNote()
    {
        var store = new PendingNoteStore();
        store.Set(botId: 1, chatId: 1, SampleNote);

        store.Clear(botId: 1, chatId: 1);

        Assert.Null(store.Get(botId: 1, chatId: 1));
    }

    [Fact]
    public void Set_KeepsNote_SeparatePerBot_EvenForTheSameChatId()
    {
        var store = new PendingNoteStore();

        store.Set(botId: 1, chatId: 1, SampleNote);
        store.Set(botId: 2, chatId: 1, OtherNote);

        Assert.Equal(SampleNote, store.Get(botId: 1, chatId: 1));
        Assert.Equal(OtherNote, store.Get(botId: 2, chatId: 1));
    }

    [Fact]
    public void Clear_OnlyAffectsTheGivenBot()
    {
        var store = new PendingNoteStore();
        store.Set(botId: 1, chatId: 1, SampleNote);
        store.Set(botId: 2, chatId: 1, OtherNote);

        store.Clear(botId: 1, chatId: 1);

        Assert.Null(store.Get(botId: 1, chatId: 1));
        Assert.Equal(OtherNote, store.Get(botId: 2, chatId: 1));
    }
}