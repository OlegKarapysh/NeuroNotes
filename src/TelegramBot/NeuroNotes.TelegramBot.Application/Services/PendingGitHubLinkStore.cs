namespace NeuroNotes.TelegramBot.Application.Services;

public sealed class PendingGitHubLinkStore : IPendingGitHubLinkStore
{
    private readonly ConcurrentDictionary<(long BotId, long ChatId), string> _repoByBotChat = new();

    public void SetRepo(long botId, long chatId, string repoInput) => _repoByBotChat[(botId, chatId)] = repoInput;

    public string? GetRepo(long botId, long chatId) => _repoByBotChat.GetValueOrDefault((botId, chatId));

    public void Clear(long botId, long chatId) => _repoByBotChat.TryRemove((botId, chatId), out _);
}