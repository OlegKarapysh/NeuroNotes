namespace NeuroNotes.Platform.Public.Messaging;

/// <summary>Published by every receiver (polling loop / webhook endpoint) in place of the raw Telegram Update.</summary>
public sealed record BotUpdate(long BotId, Update Update) : IBotScopedMessage;