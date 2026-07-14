using NeuroNotes.TelegramBot.Application.Menus;
using NeuroNotes.TelegramBot.Public;

namespace NeuroNotes.TelegramBot.UnitTests.Menus;

public class MenuKeyboardFactoryTests
{
    [Fact]
    public void HasTranscription_Keyboard_IncludesSaveToGitHubButton()
    {
        var keyboard = MenuKeyboardFactory.Build(ChatState.HasTranscription);

        Assert.Contains(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.SaveToGitHub);
    }

    [Fact]
    public void Initial_Keyboard_DoesNotIncludeSaveToGitHubButton()
    {
        var keyboard = MenuKeyboardFactory.Build(ChatState.Initial);

        Assert.DoesNotContain(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.SaveToGitHub);
    }

    [Fact]
    public void Initial_Keyboard_IncludesConnectGitHubButton()
    {
        var keyboard = MenuKeyboardFactory.Build(ChatState.Initial);

        Assert.Contains(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.ConnectGitHub);
    }

    [Fact]
    public void Initial_Keyboard_IncludesAddTagButton()
    {
        var keyboard = MenuKeyboardFactory.Build(ChatState.Initial);

        Assert.Contains(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.AddTag);
    }

    [Theory]
    [InlineData(ChatState.AwaitingGitHubRepo)]
    [InlineData(ChatState.AwaitingGitHubToken)]
    [InlineData(ChatState.AwaitingTagName)]
    public void PromptStates_Keyboards_OfferCancel(ChatState state)
    {
        var keyboard = MenuKeyboardFactory.Build(state);

        Assert.Contains(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.Cancel);
    }

    [Fact]
    public void Initial_Keyboard_IncludesListTagsButton()
    {
        var keyboard = MenuKeyboardFactory.Build(ChatState.Initial);

        Assert.Contains(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.ListTags);
    }

    [Fact]
    public void PreviewingNote_Keyboard_OffersConfirmEditAndCancel()
    {
        var keyboard = MenuKeyboardFactory.Build(ChatState.PreviewingNote);
        var buttons = keyboard.Keyboard.SelectMany(row => row).Select(button => button.Text).ToList();

        Assert.Contains(MenuButtons.ConfirmNote, buttons);
        Assert.Contains(MenuButtons.EditText, buttons);
        Assert.Contains(MenuButtons.Cancel, buttons);
    }

    [Fact]
    public void Build_HandlesEveryChatState()
    {
        foreach (var state in Enum.GetValues<ChatState>())
        {
            // Throws ArgumentOutOfRangeException if a state has no keyboard.
            _ = MenuKeyboardFactory.Build(state);
        }
    }
}