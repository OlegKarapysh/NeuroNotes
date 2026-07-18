namespace NeuroNotes.Platform.Public.Behaviors;

/// <summary>Everything a <see cref="IBotBehavior"/> needs to handle one update for one bot.</summary>
public interface IBotUpdateContext
{
    long BotId { get; }

    Update Update { get; }

    /// <summary>This bot's own Telegram client (correct token) — reply through it.</summary>
    ITelegramBotClient Client { get; }

    /// <summary>The current update's DI scope, for resolving behavior-specific dependencies.</summary>
    IServiceProvider Services { get; }
}