namespace NeuroNotes.TelegramBot.Application;

public sealed class CommandDispatcher : IConsumer<Update>
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
            else if (!message.Text.StartsWith("/"))
            {
                await context.Send(new ProcessTextMessageCommand(message));
            }
        }
        else if (message?.Voice is not null)
        {
            await context.Send(new ProcessVoiceMessageCommand(message));
        }
    }
}