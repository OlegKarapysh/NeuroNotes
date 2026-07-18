namespace NeuroNotes.Platform.Public;

/// <summary>
/// Holds the live <see cref="ITelegramBotClient"/> for each active bot, keyed by <c>BotId</c>. Populated by
/// the supervisor when a bot starts/rotates and consulted both by receivers (direct polling/webhook use) and
/// by the scoped <see cref="ITelegramBotClient"/> DI registration (resolved via <see cref="IBotContext"/>).
/// </summary>
public interface IBotClientRegistry
{
    /// <summary>The registered client for <paramref name="botId"/>. Throws if none is registered.</summary>
    ITelegramBotClient Get(long botId);

    bool TryGet(long botId, out ITelegramBotClient? client);

    /// <summary>Creates (or replaces, on rotation) the client for <paramref name="botId"/> from a plaintext token.</summary>
    void Set(long botId, string token);

    void Remove(long botId);
}