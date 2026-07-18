namespace NeuroNotes.Platform.Infrastructure.Bots;

public sealed class BotClientRegistry(IHttpClientFactory httpClientFactory) : IBotClientRegistry
{
    private readonly ConcurrentDictionary<long, ITelegramBotClient> _clientsByBotId = new();

    public ITelegramBotClient Get(long botId) =>
        _clientsByBotId.TryGetValue(botId, out var client)
            ? client
            : throw new InvalidOperationException($"No Telegram client is registered for bot {botId}.");

    public bool TryGet(long botId, out ITelegramBotClient? client) =>
        _clientsByBotId.TryGetValue(botId, out client);

    public void Set(long botId, string token)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(BotClientRegistry));
        _clientsByBotId[botId] = new TelegramBotClient(new TelegramBotClientOptions(token), httpClient);
    }

    public void Remove(long botId) => _clientsByBotId.TryRemove(botId, out _);
}