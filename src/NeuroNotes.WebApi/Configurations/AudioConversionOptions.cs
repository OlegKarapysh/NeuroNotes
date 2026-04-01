namespace NeuroNotes.WebApi.Configurations;

public sealed record AudioConversionOptions
{
    public const string SectionName = "AudioConversion";

    public string FFmpegPath { get; init; } = "ffmpeg";
    
    [Required]
    public int TimeoutSeconds { get; init; }
}