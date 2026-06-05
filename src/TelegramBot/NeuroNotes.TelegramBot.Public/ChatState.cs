namespace NeuroNotes.TelegramBot.Public;

public enum ChatState
{
    Initial,
    HasTranscription,
    AwaitingEditPrompt,
    AwaitingGitHubRepo,
    AwaitingGitHubToken,
    AwaitingTagName
}