namespace NeuroNotes.AiAssistant.Persistence.Repositories;

public sealed class PostgresTagStore(AiAssistantDbContext dbContext) : ITagStore
{
    public async Task<Result> AddAsync(long userId, string tag, CancellationToken cancellationToken = default)
    {
        var normalizedName = Normalize(tag);

        var alreadyExists = await dbContext.Tags
            .AnyAsync(t => t.UserId == userId && t.NormalizedName == normalizedName, cancellationToken);
        if (alreadyExists)
        {
            return DuplicateTagError(tag);
        }

        dbContext.Tags.Add(new TagEntity
        {
            UserId = userId,
            Name = tag,
            NormalizedName = normalizedName
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent insert beat us to the unique (UserId, NormalizedName) index.
            return DuplicateTagError(tag);
        }

        return Result.Ok();
    }

    public async Task<IReadOnlyList<string>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
        => await dbContext.Tags
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Id)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);

    private static string Normalize(string tag) => tag.ToUpperInvariant();

    private static Error DuplicateTagError(string tag) => new($"Tag \"{tag}\" already exists.");
}