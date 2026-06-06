namespace NeuroNotes.Persistence.Infrastructure.Repositories;

public sealed class PostgresUserGitHubSettingsStore(NeuroNotesDbContext dbContext) : IUserGitHubSettingsStore
{
    public async Task SaveAsync(long userId, GitHubRepositorySettings settings, CancellationToken cancellationToken = default)
    {
        // Tracked (not AsNoTracking) so the update branch below is persisted on SaveChanges.
        var entity = await dbContext.UserGitHubSettings.FindAsync([userId], cancellationToken);
        if (entity is null)
        {
            dbContext.UserGitHubSettings.Add(new UserGitHubSettingsEntity
            {
                UserId = userId,
                Owner = settings.Owner,
                Repo = settings.Repo,
                Branch = settings.Branch,
                NotesFolder = settings.NotesFolder,
                AccessToken = settings.AccessToken
            });
        }
        else
        {
            entity.Owner = settings.Owner;
            entity.Repo = settings.Repo;
            entity.Branch = settings.Branch;
            entity.NotesFolder = settings.NotesFolder;
            entity.AccessToken = settings.AccessToken;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GitHubRepositorySettings?> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserGitHubSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        return entity is null
            ? null
            : new GitHubRepositorySettings(entity.Owner, entity.Repo, entity.Branch, entity.NotesFolder, entity.AccessToken);
    }

    public async Task RemoveAsync(long userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserGitHubSettings.FindAsync([userId], cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.UserGitHubSettings.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}