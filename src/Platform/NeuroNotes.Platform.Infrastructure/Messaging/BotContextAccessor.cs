namespace NeuroNotes.Platform.Infrastructure.Messaging;

/// <summary>Scoped, settable backing implementation of <see cref="IBotContext"/> — see <see cref="BotScopeFilter{T}"/>.</summary>
public sealed class BotContextAccessor : IBotContext
{
    public long BotId { get; set; }
}