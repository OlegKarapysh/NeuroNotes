namespace NeuroNotes.TelegramBot.Application.Services;

/// <summary>
/// Holds the repository a user sent during the GitHub connect flow, between the repo step and the token step.
/// </summary>
public interface IPendingGitHubLinkStore
{
    void SetRepo(long chatId, string repoInput);
    string? GetRepo(long chatId);
    void Clear(long chatId);
}