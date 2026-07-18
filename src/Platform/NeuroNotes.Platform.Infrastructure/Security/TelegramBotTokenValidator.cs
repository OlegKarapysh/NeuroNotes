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
            // Log only the message/type, never the full exception: the token was just used to build the
            // request URI (api.telegram.org/bot<token>/getMe), which an inner exception could surface into
            // logs that are meant to stay token-free.
            logger.LogWarning("Bot token validation failed: {ExceptionType}: {Reason}", exception.GetType().Name, exception.Message);
            return new Error("Telegram rejected the bot token.");
        }
    }
}