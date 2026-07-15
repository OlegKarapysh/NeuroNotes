using NeuroNotes.AiAssistant.Public.Interfaces;
using NeuroNotes.TelegramBot.Application.Services;

namespace NeuroNotes.TelegramBot.UnitTests.Services;

public class PendingNoteStoreTests
{
    private static readonly CreatedNote SampleNote = new("note_20260714_120000.md", "---\ntitle: Test\n---\nbody", ["work"]);

    [Fact]
    public void Get_ReturnsNull_WhenNothingPending()
    {
        var store = new PendingNoteStore();

        Assert.Null(store.Get(chatId: 1));
    }

    [Fact]
    public void Set_ThenGet_ReturnsSameNote()
    {
        var store = new PendingNoteStore();

        store.Set(chatId: 1, SampleNote);

        Assert.Equal(SampleNote, store.Get(chatId: 1));
    }

    [Fact]
    public void Set_IsPerChat()
    {
        var store = new PendingNoteStore();

        store.Set(chatId: 1, SampleNote);

        Assert.Null(store.Get(chatId: 2));
    }

    [Fact]
    public void Clear_RemovesPendingNote()
    {
        var store = new PendingNoteStore();
        store.Set(chatId: 1, SampleNote);

        store.Clear(chatId: 1);

        Assert.Null(store.Get(chatId: 1));
    }
}