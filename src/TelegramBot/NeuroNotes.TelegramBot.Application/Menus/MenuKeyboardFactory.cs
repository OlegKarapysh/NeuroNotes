using Telegram.Bot.Types.ReplyMarkups;

namespace NeuroNotes.TelegramBot.Application.Menus;

public static class MenuKeyboardFactory
{
    public static ReplyKeyboardMarkup Build(ChatState state) => state switch
    {
        ChatState.Initial => new ReplyKeyboardMarkup(
        [
            [new KeyboardButton(MenuButtons.SendText), new KeyboardButton(MenuButtons.SendVoice)]
        ])
        {
            ResizeKeyboard = true,
            IsPersistent = true
        },
        ChatState.HasTranscription => new ReplyKeyboardMarkup(
        [
            [new KeyboardButton(MenuButtons.EditText), new KeyboardButton(MenuButtons.CreateNote)],
            [new KeyboardButton(MenuButtons.SendText)]
        ])
        {
            ResizeKeyboard = true,
            IsPersistent = true
        },
        ChatState.AwaitingEditPrompt => new ReplyKeyboardMarkup(
        [
            [new KeyboardButton(MenuButtons.Cancel), new KeyboardButton(MenuButtons.CreateNote)]
        ])
        {
            ResizeKeyboard = true,
            IsPersistent = true
        },
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown chat state")
    };
}
