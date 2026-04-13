namespace NeuroNotes.AudioProcessing.Infrastructure.SpeechRecognition;

public sealed record SpeechRecognitionOptions
{
    public const string SectionName = "SpeechRecognition";

    [Required]
    public string ModelFileName { get; set; } = string.Empty;
}