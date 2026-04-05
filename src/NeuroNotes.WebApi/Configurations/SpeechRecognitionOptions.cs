namespace NeuroNotes.WebApi.Configurations;

public sealed record SpeechRecognitionOptions
{
    public const string SectionName = "SpeechRecognition";

    [Required]
    public string ModelFileName { get; set; } = string.Empty;
}