using NeuroNotes.Platform.Public.Behaviors;
using Telegram.Bot;

namespace EchoBehavior;

/// <summary>
/// A minimal sample behavior extension demonstrating the Behavior SDK (see
/// specs/001-multi-bot-platform/contracts/behavior-sdk.md). Replies to every text message by echoing it
/// back. Build this project, then upload the resulting DLL via <c>POST /admin/behaviors</c> to try
/// registering a new bot behavior type on a running platform without a redeploy (quickstart Scenario 6).
/// </summary>
public sealed class EchoBotBehavior : IBotBehavior
{
    public string Key => "echo";

    public string DisplayName => "Echo";

    public string ContractVersion => BehaviorContractVersion.Current;

    public async Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken)
    {
        var message = context.Update.Message;
        if (message?.Text is null)
        {
            return;
        }

        await context.Client.SendMessage(message.Chat.Id, $"echo: {message.Text}", cancellationToken: cancellationToken);
    }
}