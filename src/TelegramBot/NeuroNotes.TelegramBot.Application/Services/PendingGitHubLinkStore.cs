using System.Collections.Concurrent;

namespace NeuroNotes.TelegramBot.Application.Services;

public sealed class PendingGitHubLinkStore : IPendingGitHubLinkStore
{
    private readonly ConcurrentDictionary<long, string> _repoByChat = new();

    public void SetRepo(long chatId, string repoInput) => _repoByChat[chatId] = repoInput;

    public string? GetRepo(long chatId) => _repoByChat.GetValueOrDefault(chatId);

    public void Clear(long chatId) => _repoByChat.TryRemove(chatId, out _);
}