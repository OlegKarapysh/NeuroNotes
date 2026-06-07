namespace NeuroNotes.TelegramBot.Persistence.Entities;

public sealed class ChatStateEntity
{
    public long ChatId { get; set; }
    public ChatState State { get; set; }
}