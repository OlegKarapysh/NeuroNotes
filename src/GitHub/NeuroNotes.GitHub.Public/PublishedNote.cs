namespace NeuroNotes.GitHub.Public;

/// <summary>
/// The result of committing a note to a repository: links to the commit and the file.
/// </summary>
public sealed record PublishedNote(string CommitUrl, string FileUrl);