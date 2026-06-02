namespace NeuroNotes.GitHub.Public;

/// <summary>
/// A user's resolved link to a GitHub repository where their notes are committed.
/// </summary>
public sealed record GitHubRepositorySettings(
    string Owner,
    string Repo,
    string Branch,
    string NotesFolder,
    string AccessToken);