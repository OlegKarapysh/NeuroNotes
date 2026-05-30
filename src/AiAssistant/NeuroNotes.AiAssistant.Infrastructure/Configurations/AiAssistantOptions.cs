using System.ComponentModel.DataAnnotations;

namespace NeuroNotes.AiAssistant.Infrastructure.Configurations;

public sealed record AiAssistantOptions
{
    public const string SectionName = "AiAssistant";

    [Required]
    public string OpenAiApiKey { get; set; } = string.Empty;
    
    [Required]
    public string DefaultModelId { get; set; } = string.Empty;
}