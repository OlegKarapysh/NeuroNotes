using Microsoft.EntityFrameworkCore.Design;

namespace NeuroNotes.Persistence.Infrastructure;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> commands (e.g. adding migrations).
/// The connection string is a placeholder — design-time tooling never opens a connection
/// unless the command targets a real database (e.g. <c>dotnet ef database update</c>).
/// </summary>
public sealed class NeuroNotesDbContextFactory : IDesignTimeDbContextFactory<NeuroNotesDbContext>
{
    public NeuroNotesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NeuroNotesDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=neuronotes;Username=neuronotes;Password=neuronotes")
            .Options;

        return new NeuroNotesDbContext(options);
    }
}