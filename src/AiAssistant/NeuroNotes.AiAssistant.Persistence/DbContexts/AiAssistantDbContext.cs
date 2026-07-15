namespace NeuroNotes.AiAssistant.Persistence.DbContexts;

public class AiAssistantDbContext(DbContextOptions<AiAssistantDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public const string Schema = "ai_assistant";

    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();
    public DbSet<NoteTagEntity> NoteTags => Set<NoteTagEntity>();

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

        modelBuilder.Entity<NoteTagEntity>(noteTag =>
        {
            noteTag.HasKey(nt => new { nt.NoteId, nt.TagId });
            noteTag.HasIndex(nt => nt.TagId);
            noteTag.HasOne(nt => nt.Note).WithMany().HasForeignKey(nt => nt.NoteId).OnDelete(DeleteBehavior.Cascade);
            noteTag.HasOne<TagEntity>().WithMany().HasForeignKey(nt => nt.TagId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}