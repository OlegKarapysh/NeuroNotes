namespace NeuroNotes.GitHub.Infrastructure;

internal sealed class OctokitGitHubNotePublisher(IGitHubClientFactory clientFactory) : IGitHubNotePublisher
{
    public async Task<Result<PublishedNote>> PublishNote(
        GitHubRepositorySettings settings,
        string fileName,
        string markdown,
        CancellationToken cancellationToken = default)
    {
        var client = clientFactory.Create(settings.AccessToken);
        var path = CombinePath(settings.NotesFolder, fileName);

        try
        {
            var existingSha = await TryGetExistingSha(client, settings, path);

            var changeSet = existingSha is null
                ? await client.Repository.Content.CreateFile(
                    settings.Owner, settings.Repo, path,
                    new CreateFileRequest($"Add note {fileName}", markdown, settings.Branch))
                : await client.Repository.Content.UpdateFile(
                    settings.Owner, settings.Repo, path,
                    new UpdateFileRequest($"Update note {fileName}", markdown, existingSha, settings.Branch));

            var commitUrl = $"https://github.com/{settings.Owner}/{settings.Repo}/commit/{changeSet.Commit.Sha}";
            return new PublishedNote(commitUrl, changeSet.Content.HtmlUrl);
        }
        catch (AuthorizationException)
        {
            return new Error("GitHub rejected the access token. Reconnect with a token that has Contents: write access.");
        }
        catch (ApiException ex)
        {
            return new Error($"GitHub could not save the note: {ex.Message}");
        }
    }

    private static async Task<string?> TryGetExistingSha(
        IGitHubClient client,
        GitHubRepositorySettings settings,
        string path)
    {
        try
        {
            var contents = await client.Repository.Content.GetAllContentsByRef(
                settings.Owner, settings.Repo, path, settings.Branch);
            return contents.Count > 0 ? contents[0].Sha : null;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static string CombinePath(string notesFolder, string fileName) =>
        string.IsNullOrWhiteSpace(notesFolder)
            ? fileName
            : $"{notesFolder.Trim('/')}/{fileName}";
}