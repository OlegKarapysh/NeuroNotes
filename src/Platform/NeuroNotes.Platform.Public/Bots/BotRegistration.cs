namespace NeuroNotes.Platform.Public.Bots;

/// <summary>
/// A hosted bot's durable, non-secret record. Never carries the bot token — see
/// <see cref="IBotRegistry.GetEncryptedTokenAsync"/> for the (still encrypted) token, used only
/// internally to build the bot's Telegram client.
/// </summary>
public sealed record BotRegistration(
    long Id,
    long TelegramBotId,
    string? Username,
    string Label,
    string BehaviorKey,
    BotStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);