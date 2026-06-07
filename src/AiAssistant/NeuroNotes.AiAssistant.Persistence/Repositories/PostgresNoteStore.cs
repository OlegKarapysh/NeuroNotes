namespace NeuroNotes.AiAssistant.Persistence.Repositories;

public sealed class PostgresNoteStore(AiAssistantDbContext dbContext) : INoteStore
{
    public async Task SaveAsync(long userId, string fileName, string content, CancellationToken cancellationToken = default)
    {
        dbContext.Notes.Add(new NoteEntity
        {
            UserId = userId,
            FileName = fileName,
            Content = content,
            SavedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredNote>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
        => await dbContext.Notes
            .AsNoTracking()
            .Where(note => note.UserId == userId)
            .OrderBy(note => note.SavedAt)
            .ThenBy(note => note.Id)
            .Select(note => new StoredNote(note.FileName, note.Content, note.SavedAt))
            .ToListAsync(cancellationToken);
}