using Microsoft.EntityFrameworkCore;
using NeuroNotes.Persistence.Infrastructure;

namespace NeuroNotes.Persistence.UnitTests;

/// <summary>
/// Creates a <see cref="NeuroNotesDbContext"/> backed by the EF Core in-memory provider,
/// so repository tests stay pure (no network, no filesystem, no real database).
/// </summary>
internal static class InMemoryDbContextFactory
{
    public static NeuroNotesDbContext Create() =>
        new(new DbContextOptionsBuilder<NeuroNotesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}