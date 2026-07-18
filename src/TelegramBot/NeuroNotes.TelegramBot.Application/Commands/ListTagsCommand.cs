namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ListTagsCommand(long BotId, Message Message) : IBotScopedMessage;

public sealed class ListTagsCommandHandler(
    ITelegramBotClient telegramBotClient,
    ITagStore tagStore,
    IChatStateStore chatStateStore) : IConsumer<ListTagsCommand>
{
    public async Task Consume(ConsumeContext<ListTagsCommand> context)
    {
        var botId = context.Message.BotId;
        var message = context.Message.Message;
        var chatId = message.Chat.Id;
        var tags = await tagStore.GetAllAsync(botId, chatId, context.CancellationToken);
        var state = await chatStateStore.GetAsync(botId, chatId, context.CancellationToken);

        var text = tags.Count == 0
            ? "You have no tags yet. Use /add-tag to create one."
            : $"Your tags:\n{string.Join("\n", tags.Select(t => $"• {t}"))}";

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: text,
            replyMarkup: MenuKeyboardFactory.Build(state),
            cancellationToken: context.CancellationToken);
    }
}