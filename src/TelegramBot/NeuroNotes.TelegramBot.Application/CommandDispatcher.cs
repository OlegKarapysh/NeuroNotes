using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application;

public sealed class CommandDispatcher(
    IChatStateStore chatStateStore,
    ITelegramBotClient telegramBotClient,
    ILogger<CommandDispatcher> logger) : IConsumer<Update>
{
    public async Task Consume(ConsumeContext<Update> context)
    {
        if (context.Message.Type is not UpdateType.Message)
        {
            return;
        }

        var message = context.Message.Message;
        if (message is null)
        {
            return;
        }

        var chatId = message.Chat.Id;
        var state = chatStateStore.Get(chatId);

        if (message.Voice is not null)
        {
            await HandleVoice(context, state, message);
            return;
        }

        if (message.Text is null)
        {
            return;
        }

        switch (message.Text)
        {
            case "/start":
                await ResetToInitial(chatId, context.CancellationToken);
                return;

            case "/create-note" or MenuButtons.CreateNote:
                await DispatchIfAllowed(
                    context, state, () => new CreateNoteCommand(message));
                return;

            case MenuButtons.EditText:
                await StartEditFlow(chatId, state, context.CancellationToken);
                return;

            case MenuButtons.Cancel:
                await CancelEditFlow(chatId, state, context.CancellationToken);
                return;

            case MenuButtons.SendText:
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: state == ChatState.AwaitingEditPrompt
                        ? "Type a message describing how to change the transcription."
                        : "Type your question and send it as a message.",
                    replyMarkup: MenuKeyboardFactory.Build(state),
                    cancellationToken: context.CancellationToken);
                return;

            case MenuButtons.SendVoice:
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "Record and send a voice message.",
                    replyMarkup: MenuKeyboardFactory.Build(state),
                    cancellationToken: context.CancellationToken);
                return;
        }

        if (message.Text.StartsWith('/'))
        {
            return;
        }

        await HandleText(context, state, message);
    }

    private async Task HandleText(ConsumeContext<Update> context, ChatState state, Message message)
    {
        if (state == ChatState.AwaitingEditPrompt)
        {
            await DispatchIfAllowed(
                context, state, () => new EditTranscriptionCommand(message, message.Text));
            return;
        }

        await DispatchIfAllowed(
            context, state, () => new ProcessTextMessageCommand(message));
    }

    private async Task HandleVoice(ConsumeContext<Update> context, ChatState state, Message message)
    {
        if (state == ChatState.AwaitingEditPrompt)
        {
            await DispatchIfAllowed(
                context, state, () => new EditTranscriptionCommand(message, TextPrompt: null));
            return;
        }

        await DispatchIfAllowed(
            context, state, () => new ProcessVoiceMessageCommand(message));
    }

    private async Task DispatchIfAllowed<TCommand>(
        ConsumeContext<Update> context,
        ChatState state,
        Func<TCommand> commandFactory)
        where TCommand : class
    {
        var chatId = context.Message.Message!.Chat.Id;

        if (!ChatStateCommandsMap.IsAllowed<TCommand>(state))
        {
            logger.LogInformation(
                "Command {Command} is not allowed in state {State} for chat {ChatId}",
                typeof(TCommand).Name, state, chatId);

            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "This action is not available right now. Pick one from the menu below.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: context.CancellationToken);
            return;
        }

        await context.Send(commandFactory());
    }

    private async Task StartEditFlow(long chatId, ChatState state, CancellationToken cancellationToken)
    {
        if (state != ChatState.HasTranscription)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "There is no transcription to edit. Send a voice message first.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: cancellationToken);
            return;
        }

        chatStateStore.Set(chatId, ChatState.AwaitingEditPrompt);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Send a text or voice message describing how you want to change the transcription.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingEditPrompt),
            cancellationToken: cancellationToken);
    }

    private async Task CancelEditFlow(long chatId, ChatState state, CancellationToken cancellationToken)
    {
        if (state != ChatState.AwaitingEditPrompt)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "Nothing to cancel.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: cancellationToken);
            return;
        }

        chatStateStore.Set(chatId, ChatState.HasTranscription);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Edit cancelled.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.HasTranscription),
            cancellationToken: cancellationToken);
    }

    private async Task ResetToInitial(long chatId, CancellationToken cancellationToken)
    {
        chatStateStore.Set(chatId, ChatState.Initial);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Welcome! Send a text question or a voice message to get started.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: cancellationToken);
    }
}
