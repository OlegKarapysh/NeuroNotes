namespace NeuroNotes.TelegramBot.Infrastructure;

/// <summary>
/// Caps how many <see cref="ProcessVoiceMessageCommandHandler"/> messages run concurrently on the in-memory bus
/// </summary>
public sealed class ProcessVoiceMessageCommandHandlerDefinition
    : ConsumerDefinition<ProcessVoiceMessageCommandHandler>
{
    public ProcessVoiceMessageCommandHandlerDefinition(IOptions<BusOptions> busOptions)
    {
        ConcurrentMessageLimit = busOptions.Value.VoiceProcessingConcurrencyLimit;
    }
}