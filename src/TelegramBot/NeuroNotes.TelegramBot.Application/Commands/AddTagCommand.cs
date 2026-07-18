namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Saves the tag name carried in <see cref="Message"/> to the user's tag store.
/// Tags can later be attached to notes.
/// </summary>
public sealed record AddTagCommand(long BotId, Message Message) : IBotScopedMessage;

public sealed class AddTagCommandHandler(
    ITelegramBotClient telegramBotClient,
    ITagStore tagStore,
    IChatStateStore chatStateStore) : IConsumer<AddTagCommand>
{
    public async Task Consume(ConsumeContext<AddTagCommand> context)
    {
        var botId = context.Message.BotId;
        var message = context.Message.Message;
        var chatId = message.Chat.Id;

        var tag = message.Text?.Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "A tag name can't be empty. Send the tag as a text message, or tap Cancel.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingTagName),
                cancellationToken: context.CancellationToken);
            return;
        }

        var addResult = await tagStore.AddAsync(botId, chatId, tag, context.CancellationToken);
        if (addResult.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: addResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingTagName),
                cancellationToken: context.CancellationToken);
            return;
        }

        await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, context.CancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: $"Tag \"{tag}\" added. You can attach it to your notes later.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: context.CancellationToken);
    }
}