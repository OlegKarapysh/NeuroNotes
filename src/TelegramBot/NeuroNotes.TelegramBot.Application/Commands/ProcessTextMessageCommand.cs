using NeuroNotes.AiAssistant.Public.Interfaces;

namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record ProcessTextMessageCommand(Message Message);

public sealed class ProcessTextMessageCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteAssistant noteAssistant) : IConsumer<ProcessTextMessageCommand>
{
    public async Task Consume(ConsumeContext<ProcessTextMessageCommand> context)
    {
        var message = context.Message.Message;
        if (message.Text is null)
        {
            return;
        }

        await telegramBotClient.SendChatAction(
            chatId: message.Chat.Id,
            action: ChatAction.Typing,
            cancellationToken: context.CancellationToken);

        var answer = await noteAssistant.Ask(message.Chat.Id, message.Text, context.CancellationToken);

        var replyText = answer.IsSuccess
            ? answer.Value
            : answer.Errors.First().Message;

        await telegramBotClient.SendMessage(
            chatId: message.Chat.Id,
            text: replyText,
            cancellationToken: context.CancellationToken);
    }
}
