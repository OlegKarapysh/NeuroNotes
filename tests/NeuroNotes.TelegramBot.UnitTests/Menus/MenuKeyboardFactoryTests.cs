using NeuroNotes.TelegramBot.Application.Menus;

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

    [Theory]
    [InlineData(ChatState.AwaitingGitHubRepo)]
    [InlineData(ChatState.AwaitingGitHubToken)]
    public void GitHubOnboarding_Keyboards_OfferCancel(ChatState state)
    {
        var keyboard = MenuKeyboardFactory.Build(state);

        Assert.Contains(
            keyboard.Keyboard.SelectMany(row => row),
            button => button.Text == MenuButtons.Cancel);
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