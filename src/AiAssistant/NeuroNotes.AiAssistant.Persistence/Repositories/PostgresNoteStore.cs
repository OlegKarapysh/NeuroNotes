namespace NeuroNotes.AiAssistant.Persistence.Repositories;

public sealed class PostgresNoteStore(AiAssistantDbContext dbContext) : INoteStore
{
    public async Task SaveAsync(long userId, string fileName, string content, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        var note = new NoteEntity
        {
            UserId = userId,
            FileName = fileName,
            Content = content,
            SavedAt = DateTime.UtcNow
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (tags.Count > 0)
        {
            await LinkTagsAsync(userId, note.Id, tags, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StoredNote>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
        => await dbContext.Notes
            .AsNoTracking()
            .Where(note => note.UserId == userId)
            .OrderBy(note => note.SavedAt)
            .ThenBy(note => note.Id)
            .Select(note => new StoredNote(note.FileName, note.Content, note.SavedAt))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Associates the saved note with the user's tags. Resolves tag names to the user's existing tags
    /// (case-insensitively, via their normalized form); unknown names are silently skipped so a stale
    /// suggestion can never invent a tag.
    /// </summary>
    private async Task LinkTagsAsync(long userId, long noteId, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var normalizedNames = tags.Select(tag => tag.ToUpperInvariant()).ToHashSet();

        var tagIds = await dbContext.Tags
            .Where(tag => tag.UserId == userId && normalizedNames.Contains(tag.NormalizedName))
            .Select(tag => tag.Id)
            .ToListAsync(cancellationToken);

        if (tagIds.Count == 0)
        {
            return;
        }

        foreach (var tagId in tagIds)
        {
            dbContext.NoteTags.Add(new NoteTagEntity { NoteId = noteId, TagId = tagId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}