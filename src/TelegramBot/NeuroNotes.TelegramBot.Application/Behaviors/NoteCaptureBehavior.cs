namespace NeuroNotes.TelegramBot.Application.Behaviors;

/// <summary>
/// The platform's built-in behavior: bridges <see cref="IBotBehavior"/> to the existing note-capture chat
/// state machine (<see cref="CommandDispatcher"/>), so the transcribe → clean up → preview → save flow is
/// preserved exactly as it was before the platform hosted multiple bots (FR-007, FR-024).
/// </summary>
public sealed class NoteCaptureBehavior(CommandDispatcher commandDispatcher) : IBotBehavior
{
    public string Key => "note-capture";

    public string DisplayName => "NeuroNotes Note Capture";

    public string ContractVersion => BehaviorContractVersion.Current;

    public Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(context.BotId, context.Update, cancellationToken);
}