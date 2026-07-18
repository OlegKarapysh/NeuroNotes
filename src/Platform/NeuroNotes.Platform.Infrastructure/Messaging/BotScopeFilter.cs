namespace NeuroNotes.Platform.Infrastructure.Messaging;

/// <summary>
/// Sets the current DI scope's <see cref="BotContextAccessor"/> from the message's <see cref="IBotScopedMessage.BotId"/>
/// before the message reaches its consumer — so a constructor-injected, bot-scoped <c>ITelegramBotClient</c>
/// resolves to the correct bot's client (D1/D2). Registered as an open-generic consume filter constrained
/// to <see cref="IBotScopedMessage"/>, so it applies to every <c>BotUpdate</c> and every Telegram command.
/// </summary>
public sealed class BotScopeFilter<T>(BotContextAccessor botContext) : IFilter<ConsumeContext<T>>
    where T : class, IBotScopedMessage
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        botContext.BotId = context.Message.BotId;
        await next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("botScope");
}