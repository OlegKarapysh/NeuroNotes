namespace NeuroNotes.Platform.Public.Bots;

/// <summary>
/// The durable store of registered bots (schema <c>platform</c>). Pure persistence — validation
/// (behavior catalog lookup, Telegram token check) is the caller's (<c>BotRegistrationService</c>) job.
/// </summary>
public interface IBotRegistry
{
    /// <summary>Fails if <paramref name="telegramBotId"/> is already registered (FR-011).</summary>
    Task<Result<BotRegistration>> AddAsync(
        long telegramBotId,
        string? username,
        string label,
        string behaviorKey,
        byte[] encryptedToken,
        CancellationToken cancellationToken = default);

    Task<BotRegistration?> GetAsync(long botId, CancellationToken cancellationToken = default);

    /// <summary>The bot's token ciphertext, for internal use only (building its Telegram client). Never exposed via the admin API.</summary>
    Task<byte[]?> GetEncryptedTokenAsync(long botId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotRegistration>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result> UpdateStatusAsync(long botId, BotStatus status, CancellationToken cancellationToken = default);

    /// <summary>Fails if <paramref name="telegramBotId"/> does not match the bot already registered under <paramref name="botId"/>.</summary>
    Task<Result> UpdateTokenAsync(
        long botId, long telegramBotId, byte[] encryptedToken, CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(long botId, CancellationToken cancellationToken = default);
}