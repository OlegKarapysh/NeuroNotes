namespace NeuroNotes.TelegramBot.Public;

public enum ChatState
{
    Initial,
    HasTranscription,
    AwaitingEditPrompt,
    AwaitingGitHubRepo,
    AwaitingGitHubToken,
    AwaitingTagName,

    // Appended last on purpose: ChatState is persisted as a string (see the telegram_bot migration),
    // but keep new values at the end so any int-based storage stays stable too.
    PreviewingNote
}