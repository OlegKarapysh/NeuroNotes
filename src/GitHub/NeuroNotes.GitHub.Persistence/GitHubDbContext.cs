namespace NeuroNotes.GitHub.Persistence;

public sealed class GitHubDbContext(DbContextOptions<GitHubDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Postgres schema this module owns. A dedicated schema (plus its own <c>__EFMigrationsHistory</c>
    /// table, which EF places here by default) isolates the GitHub tables from the other modules
    /// sharing the same database.
    /// </summary>
    public const string Schema = "github";

    public DbSet<UserGitHubSettingsEntity> UserGitHubSettings => Set<UserGitHubSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UserGitHubSettingsEntity>(settings =>
        {
            settings.HasKey(s => s.UserId);
            settings.Property(s => s.UserId).ValueGeneratedNever();
        });
    }
}