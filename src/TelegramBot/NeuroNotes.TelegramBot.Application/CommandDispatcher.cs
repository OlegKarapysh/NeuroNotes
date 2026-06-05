using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application;

public sealed class CommandDispatcher(
    IChatStateStore chatStateStore,
    IPendingGitHubLinkStore pendingGitHubLinkStore,
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
        var state = await chatStateStore.GetAsync(chatId, context.CancellationToken);

        if (state is ChatState.AwaitingGitHubRepo or ChatState.AwaitingGitHubToken)
        {
            await HandleGitHubOnboarding(context, state, message);
            return;
        }

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

            case "/save-to-github" or MenuButtons.SaveToGitHub:
                await DispatchIfAllowed(
                    context, state, () => new PushNoteToGitHubCommand(message));
                return;

            case "/connect-github" or MenuButtons.ConnectGitHub:
                await StartGitHubConnectFlow(chatId, context.CancellationToken);
                return;

            case "/add-tag" or MenuButtons.AddTag:
                await StartAddTagFlow(chatId, context.CancellationToken);
                return;

            case "/list-tags" or MenuButtons.ListTags:
                await DispatchIfAllowed(
                    context, state, () => new ListTagsCommand(message));
                return;

            case MenuButtons.EditText:
                await StartEditFlow(chatId, state, context.CancellationToken);
                return;

            case MenuButtons.Cancel when state == ChatState.AwaitingTagName:
                await CancelAddTagFlow(chatId, context.CancellationToken);
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

        if (state == ChatState.AwaitingTagName)
        {
            await DispatchIfAllowed(
                context, state, () => new AddTagCommand(message));
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

        await chatStateStore.SetAsync(chatId, ChatState.AwaitingEditPrompt, cancellationToken);

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

        await chatStateStore.SetAsync(chatId, ChatState.HasTranscription, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Edit cancelled.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.HasTranscription),
            cancellationToken: cancellationToken);
    }

    private async Task StartAddTagFlow(long chatId, CancellationToken cancellationToken)
    {
        await chatStateStore.SetAsync(chatId, ChatState.AwaitingTagName, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Send the name of the tag you want to add.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingTagName),
            cancellationToken: cancellationToken);
    }

    private async Task CancelAddTagFlow(long chatId, CancellationToken cancellationToken)
    {
        await chatStateStore.SetAsync(chatId, ChatState.Initial, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Tag creation cancelled.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: cancellationToken);
    }

    private async Task ResetToInitial(long chatId, CancellationToken cancellationToken)
    {
        await chatStateStore.SetAsync(chatId, ChatState.Initial, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Welcome! Send a text question or a voice message to get started.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: cancellationToken);
    }

    private async Task StartGitHubConnectFlow(long chatId, CancellationToken cancellationToken)
    {
        pendingGitHubLinkStore.Clear(chatId);
        await chatStateStore.SetAsync(chatId, ChatState.AwaitingGitHubRepo, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Let's connect a GitHub repository for your notes.\n\n"
                  + "Send the repository URL, for example:\nhttps://github.com/owner/repo",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingGitHubRepo),
            cancellationToken: cancellationToken);
    }

    private async Task HandleGitHubOnboarding(ConsumeContext<Update> context, ChatState state, Message message)
    {
        var chatId = message.Chat.Id;
        var text = message.Text;

        if (text is "/start" or MenuButtons.Cancel)
        {
            pendingGitHubLinkStore.Clear(chatId);

            if (text == "/start")
            {
                await ResetToInitial(chatId, context.CancellationToken);
                return;
            }

            await chatStateStore.SetAsync(chatId, ChatState.Initial, context.CancellationToken);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "GitHub setup cancelled.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "Please send the requested value as a text message, or tap Cancel.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: context.CancellationToken);
            return;
        }

        if (state == ChatState.AwaitingGitHubRepo)
        {
            await CaptureGitHubRepo(context, chatId, text);
            return;
        }

        await CaptureGitHubToken(context, message, chatId, text);
    }

    private async Task CaptureGitHubRepo(ConsumeContext<Update> context, long chatId, string repoInput)
    {
        pendingGitHubLinkStore.SetRepo(chatId, repoInput.Trim());
        await chatStateStore.SetAsync(chatId, ChatState.AwaitingGitHubToken, context.CancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Now send a GitHub personal access token with \"Contents: Read and write\" permission on that repository.\n\n"
                  + "Use a fine-grained token scoped to just this repo. I'll delete your token message right after reading it, "
                  + "and you can revoke the token anytime in GitHub settings.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingGitHubToken),
            cancellationToken: context.CancellationToken);
    }

    private async Task CaptureGitHubToken(ConsumeContext<Update> context, Message message, long chatId, string token)
    {
        var repoInput = pendingGitHubLinkStore.GetRepo(chatId);
        if (repoInput is null)
        {
            await chatStateStore.SetAsync(chatId, ChatState.Initial, context.CancellationToken);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "The GitHub setup expired. Please start again with /connect-github.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        await DeleteTokenMessageSafe(chatId, message.MessageId, context.CancellationToken);
        pendingGitHubLinkStore.Clear(chatId);

        await DispatchIfAllowed(
            context, ChatState.AwaitingGitHubToken, () => new ConnectGitHubCommand(message, repoInput, token.Trim()));
    }

    private async Task DeleteTokenMessageSafe(long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            await telegramBotClient.DeleteMessage(chatId, messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete the GitHub token message {MessageId} in chat {ChatId}", messageId, chatId);
        }
    }
}