namespace NeuroNotes.Platform.Public.Messaging;

/// <summary>Marks a bus message as belonging to a specific hosted bot.</summary>
public interface IBotScopedMessage
{
    long BotId { get; }
}