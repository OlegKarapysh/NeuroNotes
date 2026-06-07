using Microsoft.EntityFrameworkCore;
using NeuroNotes.AiAssistant.Persistence;

namespace NeuroNotes.AiAssistant.UnitTests;

/// <summary>
/// Creates an <see cref="AiAssistantDbContext"/> backed by the EF Core in-memory provider,
/// so repository tests stay pure (no network, no filesystem, no real database).
/// </summary>
internal static class InMemoryDbContextFactory
{
    public static AiAssistantDbContext Create() =>
        new(new DbContextOptionsBuilder<AiAssistantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}