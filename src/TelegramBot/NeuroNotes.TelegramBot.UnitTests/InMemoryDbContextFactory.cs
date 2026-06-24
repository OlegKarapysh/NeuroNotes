using Microsoft.EntityFrameworkCore;
using NeuroNotes.TelegramBot.Persistence;

namespace NeuroNotes.TelegramBot.UnitTests;

/// <summary>
/// Creates a <see cref="TelegramBotDbContext"/> backed by the EF Core in-memory provider,
/// so repository tests stay pure (no network, no filesystem, no real database).
/// </summary>
internal static class InMemoryDbContextFactory
{
    public static TelegramBotDbContext Create() =>
        new(new DbContextOptionsBuilder<TelegramBotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}