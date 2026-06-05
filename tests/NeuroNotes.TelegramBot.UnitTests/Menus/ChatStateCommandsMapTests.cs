using NeuroNotes.TelegramBot.Application.Commands;
using NeuroNotes.TelegramBot.Application.Menus;
using NeuroNotes.TelegramBot.Public;

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
    public void AddTag_IsAllowed_WhenAwaitingTagName()
    {
        Assert.True(ChatStateCommandsMap.IsAllowed<AddTagCommand>(ChatState.AwaitingTagName));
    }

    [Theory]
    [InlineData(ChatState.Initial)]
    [InlineData(ChatState.HasTranscription)]
    public void AddTag_IsNotAllowed_OutsideTagFlow(ChatState state)
    {
        Assert.False(ChatStateCommandsMap.IsAllowed<AddTagCommand>(state));
    }

    [Fact]
    public void ListTags_IsAllowed_InInitialState()
    {
        Assert.True(ChatStateCommandsMap.IsAllowed<ListTagsCommand>(ChatState.Initial));
    }

    [Theory]
    [InlineData(ChatState.HasTranscription)]
    [InlineData(ChatState.AwaitingTagName)]
    public void ListTags_IsNotAllowed_OutsideInitialState(ChatState state)
    {
        Assert.False(ChatStateCommandsMap.IsAllowed<ListTagsCommand>(state));
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