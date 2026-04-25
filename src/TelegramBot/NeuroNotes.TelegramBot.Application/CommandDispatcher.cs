namespace NeuroNotes.TelegramBot.Application;

public sealed class CommandDispatcher(ITelegramBotClient telegramBotClient) : IConsumer<Update>
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
            if (message.Text == "/create-note")
            {
                await context.Send(new CreateNoteCommand(message));
            }
            else
            {
                await telegramBotClient.SendMessage(message.Chat.Id, message.Text);
            }
        }
        else if (message?.Voice is not null)
        {
            await context.Send(new ProcessVoiceMessageCommand(message));
        }
    }
}