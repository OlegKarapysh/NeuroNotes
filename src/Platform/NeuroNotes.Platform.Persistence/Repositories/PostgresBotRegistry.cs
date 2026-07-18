namespace NeuroNotes.Platform.Persistence.Repositories;

public sealed class PostgresBotRegistry(PlatformDbContext dbContext) : IBotRegistry
{
    public async Task<Result<BotRegistration>> AddAsync(
        long telegramBotId,
        string? username,
        string label,
        string behaviorKey,
        byte[] encryptedToken,
        CancellationToken cancellationToken = default)
    {
        var alreadyExists = await dbContext.Bots
            .AnyAsync(b => b.TelegramBotId == telegramBotId, cancellationToken);
        if (alreadyExists)
        {
            return DuplicateError(telegramBotId);
        }

        var now = DateTime.UtcNow;
        var entity = new BotRegistrationEntity
        {
            TelegramBotId = telegramBotId,
            Username = username,
            Label = label,
            BehaviorKey = behaviorKey,
            EncryptedToken = encryptedToken,
            Status = BotStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Bots.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent insert beat us to the unique TelegramBotId index.
            return DuplicateError(telegramBotId);
        }

        return ToDto(entity);
    }

    public async Task<BotRegistration?> GetAsync(long botId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Bots
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == botId, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public Task<byte[]?> GetEncryptedTokenAsync(long botId, CancellationToken cancellationToken = default) =>
        dbContext.Bots
            .AsNoTracking()
            .Where(b => b.Id == botId)
            .Select(b => b.EncryptedToken)
            .SingleOrDefaultAsync(cancellationToken)!;

    public async Task<IReadOnlyList<BotRegistration>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Bots
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToArray();
    }

    public async Task<Result> UpdateStatusAsync(long botId, BotStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Bots.FindAsync([botId], cancellationToken);
        if (entity is null)
        {
            return NotFoundError(botId);
        }

        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> UpdateTokenAsync(
        long botId, long telegramBotId, byte[] encryptedToken, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Bots.FindAsync([botId], cancellationToken);
        if (entity is null)
        {
            return NotFoundError(botId);
        }

        if (entity.TelegramBotId != telegramBotId)
        {
            return new Error("The new token belongs to a different Telegram bot than the one currently registered.");
        }

        entity.EncryptedToken = encryptedToken;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> RemoveAsync(long botId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Bots.FindAsync([botId], cancellationToken);
        if (entity is null)
        {
            return NotFoundError(botId);
        }

        dbContext.Bots.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private static BotRegistration ToDto(BotRegistrationEntity entity) => new(
        entity.Id,
        entity.TelegramBotId,
        entity.Username,
        entity.Label,
        entity.BehaviorKey,
        entity.Status,
        entity.CreatedAt,
        entity.UpdatedAt);

    private static Error DuplicateError(long telegramBotId) =>
        new($"A bot for Telegram bot id {telegramBotId} is already registered.");

    private static Error NotFoundError(long botId) => new($"Bot {botId} was not found.");
}