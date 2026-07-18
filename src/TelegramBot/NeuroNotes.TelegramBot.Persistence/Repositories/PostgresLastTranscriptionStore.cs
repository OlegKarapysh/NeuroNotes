namespace NeuroNotes.TelegramBot.Persistence.Repositories;

public sealed class PostgresLastTranscriptionStore(TelegramBotDbContext dbContext) : ILastTranscriptionStore
{
    public async Task SaveAsync(long botId, long chatId, string transcription, CancellationToken cancellationToken = default)
    {
        // Tracked (not AsNoTracking) so the update branch below is persisted on SaveChanges.
        var entity = await dbContext.LastTranscriptions.FindAsync([botId, chatId], cancellationToken);
        if (entity is null)
        {
            dbContext.LastTranscriptions.Add(new LastTranscriptionEntity { BotId = botId, ChatId = chatId, Transcription = transcription });
        }
        else
        {
            entity.Transcription = transcription;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(long botId, long chatId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.LastTranscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.BotId == botId && t.ChatId == chatId, cancellationToken);

        return entity?.Transcription;
    }
}