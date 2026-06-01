using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application.Services;

public interface IChatStateStore
{
    ChatState Get(long chatId);
    void Set(long chatId, ChatState state);
}