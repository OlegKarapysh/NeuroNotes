namespace NeuroNotes.TelegramBot.Persistence;

public sealed class TelegramBotDbContext(DbContextOptions<TelegramBotDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Postgres schema this module owns. A dedicated schema (plus its own <c>__EFMigrationsHistory</c>
    /// table, which EF places here by default) isolates the TelegramBot tables from the other modules
    /// sharing the same database.
    /// </summary>
    public const string Schema = "telegram_bot";

    public DbSet<ChatStateEntity> ChatStates => Set<ChatStateEntity>();
    public DbSet<LastTranscriptionEntity> LastTranscriptions => Set<LastTranscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ChatStateEntity>(chatState =>
        {
            chatState.HasKey(c => c.ChatId);
            chatState.Property(c => c.ChatId).ValueGeneratedNever();
            chatState.Property(c => c.State).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<LastTranscriptionEntity>(transcription =>
        {
            transcription.HasKey(t => t.ChatId);
            transcription.Property(t => t.ChatId).ValueGeneratedNever();
        });
    }
}