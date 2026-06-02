namespace NeuroNotes.Persistence.Infrastructure.Entities;

public sealed class TagEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string Name { get; set; }

    /// <summary>Upper-invariant form of <see cref="Name"/>, used for case-insensitive uniqueness.</summary>
    public required string NormalizedName { get; set; }
}