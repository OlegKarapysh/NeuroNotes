namespace NeuroNotes.Persistence.Infrastructure;

// Not sealed: unit tests subclass this to simulate persistence failures (e.g. a unique-index
// violation surfacing as a DbUpdateException) that the EF in-memory provider cannot reproduce.
public class NeuroNotesDbContext(DbContextOptions<NeuroNotesDbContext> options) : DbContext(options)
{
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();
    public DbSet<UserGitHubSettingsEntity> UserGitHubSettings => Set<UserGitHubSettingsEntity>();
    public DbSet<ChatStateEntity> ChatStates => Set<ChatStateEntity>();
    public DbSet<LastTranscriptionEntity> LastTranscriptions => Set<LastTranscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<UserGitHubSettingsEntity>(settings =>
        {
            settings.HasKey(s => s.UserId);
            settings.Property(s => s.UserId).ValueGeneratedNever();
        });

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