namespace NeuroNotes.Persistence.Infrastructure.Entities;

public sealed class ChatStateEntity
{
    public long ChatId { get; set; }
    public ChatState State { get; set; }
}