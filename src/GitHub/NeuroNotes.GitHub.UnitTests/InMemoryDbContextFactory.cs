using Microsoft.EntityFrameworkCore;
using NeuroNotes.GitHub.Persistence;

namespace NeuroNotes.GitHub.UnitTests;

/// <summary>
/// Creates a <see cref="GitHubDbContext"/> backed by the EF Core in-memory provider,
/// so repository tests stay pure (no network, no filesystem, no real database).
/// </summary>
internal static class InMemoryDbContextFactory
{
    public static GitHubDbContext Create() =>
        new(new DbContextOptionsBuilder<GitHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}