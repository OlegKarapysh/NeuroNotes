using NeuroNotes.AiAssistant.Public.Interfaces;
using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ListTagsCommand(Message Message);

public sealed class ListTagsCommandHandler(
    ITelegramBotClient telegramBotClient,
    ITagStore tagStore,
    IChatStateStore chatStateStore) : IConsumer<ListTagsCommand>
{
    public async Task Consume(ConsumeContext<ListTagsCommand> context)
    {
        var message = context.Message.Message;
        var chatId = message.Chat.Id;
        var tags = tagStore.GetAll(chatId);
        var state = chatStateStore.Get(chatId);

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