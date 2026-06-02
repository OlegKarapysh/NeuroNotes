namespace NeuroNotes.GitHub.Public.Interfaces;

public interface IGitHubNotePublisher
{
    Task<Result<PublishedNote>> PublishNote(
        GitHubRepositorySettings settings,
        string fileName,
        string markdown,
        CancellationToken cancellationToken = default);
}