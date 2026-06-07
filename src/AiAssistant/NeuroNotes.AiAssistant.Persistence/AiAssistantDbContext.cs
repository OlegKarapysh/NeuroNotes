namespace NeuroNotes.AiAssistant.Persistence;

// Not sealed: unit tests subclass this to simulate persistence failures (e.g. a unique-index
// violation surfacing as a DbUpdateException) that the EF in-memory provider cannot reproduce.
public class AiAssistantDbContext(DbContextOptions<AiAssistantDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Postgres schema this module owns. A dedicated schema (plus its own <c>__EFMigrationsHistory</c>
    /// table, which EF places here by default) isolates the AiAssistant tables from the other modules
    /// sharing the same database.
    /// </summary>
    public const string Schema = "ai_assistant";

    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<NoteEntity>(note =>
        {
            note.HasKey(n => n.Id);
            note.HasIndex(n => n.UserId);
            note.Property(n => n.FileName).HasMaxLength(256);
        });

        modelBuilder.Entity<TagEntity>(tag =>
        {
            tag.HasKey(t => t.Id);
            tag.HasIndex(t => new { t.UserId, t.NormalizedName }).IsUnique();
            tag.Property(t => t.Name).HasMaxLength(128);
            tag.Property(t => t.NormalizedName).HasMaxLength(128);
        });
    }
}