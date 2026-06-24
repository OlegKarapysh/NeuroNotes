namespace NeuroNotes.AiAssistant.Persistence.Configurations;

public sealed record PersistenceOptions
{
    public const string SectionName = "Persistence";

    [Required]
    public required string ConnectionString { get; init; }
}