using NeuroNotes.TelegramBot.Application.Commands;
using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.UnitTests.Menus;

public class ChatStateCommandsMapTests
{
    [Theory]
    [InlineData(ChatState.HasTranscription)]
    [InlineData(ChatState.AwaitingEditPrompt)]
    public void PushNoteToGitHub_IsAllowed_WhenATranscriptionExists(ChatState state)
    {
        Assert.True(ChatStateCommandsMap.IsAllowed<PushNoteToGitHubCommand>(state));
    }

    [Fact]
    public void PushNoteToGitHub_IsNotAllowed_InInitialState()
    {
        Assert.False(ChatStateCommandsMap.IsAllowed<PushNoteToGitHubCommand>(ChatState.Initial));
    }

    [Fact]
    public void ConnectGitHub_IsAllowed_WhenAwaitingToken()
    {
        Assert.True(ChatStateCommandsMap.IsAllowed<ConnectGitHubCommand>(ChatState.AwaitingGitHubToken));
    }

    [Fact]
    public void ConnectGitHub_IsNotAllowed_InInitialState()
    {
        Assert.False(ChatStateCommandsMap.IsAllowed<ConnectGitHubCommand>(ChatState.Initial));
    }

    [Fact]
    public void EveryChatState_IsMapped()
    {
        foreach (var state in Enum.GetValues<ChatState>())
        {
            // Throws KeyNotFoundException if a state is missing from the map.
            _ = ChatStateCommandsMap.IsAllowed<CreateNoteCommand>(state);
        }
    }
}