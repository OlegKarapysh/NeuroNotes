namespace NeuroNotes.TelegramBot.Application.Menus;

public enum ChatState
{
    Initial,
    HasTranscription,
    AwaitingEditPrompt
}
