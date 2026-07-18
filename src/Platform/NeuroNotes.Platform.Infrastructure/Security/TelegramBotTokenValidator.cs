namespace NeuroNotes.Platform.Infrastructure.Security;

public sealed class TelegramBotTokenValidator(IHttpClientFactory httpClientFactory, ILogger<TelegramBotTokenValidator> logger)
    : IBotTokenValidator
{
    public async Task<Result<(long TelegramBotId, string? Username)>> Validate(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient(nameof(TelegramBotTokenValidator));
            var client = new TelegramBotClient(new TelegramBotClientOptions(token), httpClient);
            var me = await client.GetMe(cancellationToken);
            return (me.Id, me.Username);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Bot token validation failed.");
            return new Error("Telegram rejected the bot token.");
        }
    }
}