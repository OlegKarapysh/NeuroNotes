namespace NeuroNotes.Persistence.Infrastructure.Repositories;

public sealed class PostgresChatStateStore(NeuroNotesDbContext dbContext) : IChatStateStore
{
    public async Task<ChatState> GetAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ChatStates
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.ChatId == chatId, cancellationToken);

        return entity?.State ?? ChatState.Initial;
    }

    public async Task SetAsync(long chatId, ChatState state, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ChatStates.FindAsync([chatId], cancellationToken);
        if (entity is null)
        {
            dbContext.ChatStates.Add(new ChatStateEntity { ChatId = chatId, State = state });
        }
        else
        {
            entity.State = state;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}