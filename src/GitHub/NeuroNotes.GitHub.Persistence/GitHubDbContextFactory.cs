namespace NeuroNotes.GitHub.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> commands. No credentials are hardcoded:
/// model-only commands (e.g. <c>dotnet ef migrations add</c>) never open a connection, so the
/// password-less fallback suffices. For commands that DO connect (e.g. <c>dotnet ef database
/// update</c>), set the <c>Persistence__ConnectionString</c> environment variable.
/// </summary>
public sealed class GitHubDbContextFactory : IDesignTimeDbContextFactory<GitHubDbContext>
{
    public GitHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Persistence__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=neuronotes;Username=neuronotes";

        var options = new DbContextOptionsBuilder<GitHubDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GitHubDbContext(options);
    }
}