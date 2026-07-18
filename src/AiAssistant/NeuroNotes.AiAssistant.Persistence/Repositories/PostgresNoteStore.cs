namespace NeuroNotes.AiAssistant.Persistence.Repositories;

public sealed class PostgresNoteStore(AiAssistantDbContext dbContext) : INoteStore
{
    public async Task SaveAsync(long botId, long userId, string fileName, string content, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        var note = new NoteEntity
        {
            BotId = botId,
            UserId = userId,
            FileName = fileName,
            Content = content,
            SavedAt = DateTime.UtcNow
        };

        dbContext.Notes.Add(note);

        if (tags.Count > 0)
        {
            // Resolve the tag names to the user's existing tags within this bot's data context
            // (case-insensitively, via their normalized form); unknown names are silently skipped
            // so a stale suggestion can never invent a tag.
            var tagIds = await ResolveTagIds(botId, userId, tags, cancellationToken);

            // Add the join rows through the Note navigation so the note and its links insert in a single
            // SaveChanges (one transaction): either both persist or neither does — no divergence between the
            // saved .md and the NoteTags table.
            foreach (var tagId in tagIds)
            {
                dbContext.NoteTags.Add(new NoteTagEntity { Note = note, TagId = tagId });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredNote>> GetAllAsync(long botId, long userId, CancellationToken cancellationToken = default)
        => await dbContext.Notes
            .AsNoTracking()
            .Where(note => note.BotId == botId && note.UserId == userId)
            .OrderBy(note => note.SavedAt)
            .ThenBy(note => note.Id)
            .Select(note => new StoredNote(note.FileName, note.Content, note.SavedAt))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<long>> ResolveTagIds(long botId, long userId, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var normalizedNames = tags.Select(tag => tag.ToUpperInvariant()).ToHashSet();

        return await dbContext.Tags
            .Where(tag => tag.BotId == botId && tag.UserId == userId && normalizedNames.Contains(tag.NormalizedName))
            .Select(tag => tag.Id)
            .ToListAsync(cancellationToken);
    }
}