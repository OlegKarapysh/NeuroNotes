namespace NeuroNotes.Platform.Infrastructure.Receivers;

/// <summary>
/// Deterministically derives each bot's Telegram webhook secret token (validated via the
/// <c>X-Telegram-Bot-Api-Secret-Token</c> header on every webhook POST) from the platform's admin key,
/// so no extra secret needs to be stored per bot.
/// </summary>
public sealed class WebhookSecretProvider(IOptions<PlatformOptions> platformOptions)
{
    private const string Purpose = "NeuroNotes.Platform.WebhookSecret.v1:";

    public string GetSecret(long botId)
    {
        var key = Encoding.UTF8.GetBytes(Purpose + platformOptions.Value.AdminApiKey);
        var data = Encoding.UTF8.GetBytes(botId.ToString(CultureInfo.InvariantCulture));

        // Uppercase hex is within Telegram's allowed secret-token charset (A-Z, a-z, 0-9, _, -).
        return Convert.ToHexString(HMACSHA256.HashData(key, data));
    }
}