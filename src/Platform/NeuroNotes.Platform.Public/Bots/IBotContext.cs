namespace NeuroNotes.Platform.Public.Bots;

/// <summary>
/// Scoped ambient accessor for the bot that owns the message currently being processed. Populated by
/// <c>BotScopeFilter</c> before the consumer (and its constructor-injected, bot-scoped services such as
/// <c>ITelegramBotClient</c>) is resolved from the same DI scope.
/// </summary>
public interface IBotContext
{
    long BotId { get; }
}