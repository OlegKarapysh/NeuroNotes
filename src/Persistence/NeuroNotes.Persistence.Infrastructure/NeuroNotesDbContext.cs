namespace NeuroNotes.Persistence.Infrastructure;

public sealed class NeuroNotesDbContext(DbContextOptions<NeuroNotesDbContext> options) : DbContext(options)
{
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();
    public DbSet<UserGitHubSettingsEntity> UserGitHubSettings => Set<UserGitHubSettingsEntity>();

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
    }
}