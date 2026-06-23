namespace NeuroNotes.TelegramBot.Infrastructure;

public sealed record BusOptions
{
    public const string SectionName = "Bus";

    [Range(1, int.MaxValue)]
    public int VoiceProcessingConcurrencyLimit { get; init; } = 10;
}