namespace NeuroNotes.GitHub.Infrastructure.Configurations;

public sealed record GitHubOptions
{
    public const string SectionName = "GitHub";

    /// <summary>User-Agent product name Octokit sends with each request.</summary>
    [Required]
    public string ProductHeader { get; set; } = "NeuroNotes";

    /// <summary>Fallback branch used when a repository has no detectable default branch.</summary>
    [Required]
    public string DefaultBranch { get; set; } = "main";

    /// <summary>Folder within the repository where note files are committed.</summary>
    [Required]
    public string NotesFolder { get; set; } = "notes";
}