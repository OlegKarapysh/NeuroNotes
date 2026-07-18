namespace NeuroNotes.TelegramBot.Application.Behaviors;

/// <summary>
/// The platform's built-in behavior: bridges <see cref="IBotBehavior"/> to the existing note-capture chat
/// state machine (<see cref="CommandDispatcher"/>), so the transcribe → clean up → preview → save flow is
/// preserved exactly as it was before the platform hosted multiple bots (FR-007, FR-024).
/// </summary>
public sealed class NoteCaptureBehavior : IBotBehavior
{
    public string Key => "note-capture";

    public string DisplayName => "NeuroNotes Note Capture";

    public string ContractVersion => BehaviorContractVersion.Current;

    // The CommandDispatcher is resolved per update from the update's own DI scope (not captured in the
    // constructor): it depends on the scoped, bot-bound ITelegramBotClient, which only resolves correctly
    // once BotScopeFilter has set the current bot for this update — never at startup when this behavior is
    // first constructed and no bot's client exists yet.
    public Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken) =>
        context.Services.GetRequiredService<CommandDispatcher>().Dispatch(context.BotId, context.Update, cancellationToken);
}