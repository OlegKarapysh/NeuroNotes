using MassTransit;
using NeuroNotes.TelegramBot.Application.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NeuroNotes.TelegramBot.Application;

public sealed class TelegramMessageHandler(ITelegramBotClient telegramBotClient) : IConsumer<Update>
{
    public async Task Consume(ConsumeContext<Update> context)
    {
        if (context.Message.Type is not UpdateType.Message)
        {
            return;
        }

        var message = context.Message.Message;
        if (message?.Text is not null)
        {
            await telegramBotClient.SendMessage(message.Chat.Id, message.Text);
        }
        else if (message?.Voice is not null)
        {
            await context.Send(new ProcessVoiceMessageCommand(message));
        }
    }
}