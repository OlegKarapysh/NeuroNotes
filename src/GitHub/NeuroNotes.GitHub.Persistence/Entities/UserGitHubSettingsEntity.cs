namespace NeuroNotes.GitHub.Persistence.Entities;

public sealed class UserGitHubSettingsEntity
{
    public long UserId { get; set; }
    public required string Owner { get; set; }
    public required string Repo { get; set; }
    public required string Branch { get; set; }
    public required string NotesFolder { get; set; }
    public required string AccessToken { get; set; }
}