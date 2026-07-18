using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace NeuroNotes.Platform.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    /// <summary>
    /// Postgres schema this module owns. A dedicated schema (plus its own <c>__EFMigrationsHistory</c>
    /// table, which EF places here by default) isolates the Platform tables from the other modules
    /// sharing the same database.
    /// </summary>
    public const string Schema = "platform";

    public DbSet<BotRegistrationEntity> Bots => Set<BotRegistrationEntity>();

    /// <summary>Data Protection key ring (see <c>ITokenProtector</c>), persisted here so bot tokens stay decryptable across restarts/redeploys.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<BotRegistrationEntity>(bot =>
        {
            bot.HasKey(b => b.Id);
            bot.HasIndex(b => b.TelegramBotId).IsUnique();
            bot.Property(b => b.Label).HasMaxLength(128);
            bot.Property(b => b.BehaviorKey).HasMaxLength(128);
            bot.Property(b => b.Status).HasConversion<string>().HasMaxLength(32);
        });
    }
}