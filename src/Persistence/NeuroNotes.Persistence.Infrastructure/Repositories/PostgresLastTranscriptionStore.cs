namespace NeuroNotes.Persistence.Infrastructure.Repositories;

public sealed class PostgresLastTranscriptionStore(NeuroNotesDbContext dbContext) : ILastTranscriptionStore
{
    public async Task SaveAsync(long chatId, string transcription, CancellationToken cancellationToken = default)
    {
        // Tracked (not AsNoTracking) so the update branch below is persisted on SaveChanges.
        var entity = await dbContext.LastTranscriptions.FindAsync([chatId], cancellationToken);
        if (entity is null)
        {
            dbContext.LastTranscriptions.Add(new LastTranscriptionEntity { ChatId = chatId, Transcription = transcription });
        }
        else
        {
            entity.Transcription = transcription;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.LastTranscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.ChatId == chatId, cancellationToken);

        return entity?.Transcription;
    }
}