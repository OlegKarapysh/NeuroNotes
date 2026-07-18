namespace NeuroNotes.TelegramBot.Application;

/// <summary>
/// The note-capture chat state machine: routes an incoming <see cref="Update"/> to the right command for
/// its bot's chat state, or drives an inline onboarding flow (GitHub connect, add-tag) directly. Invoked by
/// <see cref="NoteCaptureBehavior"/> for the bot identified by <paramref name="botId"/> on every call, so
/// the same person's conversations with two different bots never share state (FR-018).
/// </summary>
public sealed class CommandDispatcher(
    IChatStateStore chatStateStore,
    IPendingGitHubLinkStore pendingGitHubLinkStore,
    IPendingNoteStore pendingNoteStore,
    ITelegramBotClient telegramBotClient,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<CommandDispatcher> logger)
{
    public async Task Dispatch(long botId, Update update, CancellationToken cancellationToken)
    {
        if (update.Type is not UpdateType.Message)
        {
            return;
        }

        var message = update.Message;
        if (message is null)
        {
            return;
        }

        var chatId = message.Chat.Id;
        var state = await chatStateStore.GetAsync(botId, chatId, cancellationToken);

        if (state is ChatState.AwaitingGitHubRepo or ChatState.AwaitingGitHubToken)
        {
            await HandleGitHubOnboarding(botId, state, message, cancellationToken);
            return;
        }

        if (message.Voice is not null)
        {
            await HandleVoice(botId, state, message, cancellationToken);
            return;
        }

        if (message.Text is null)
        {
            return;
        }

        switch (message.Text)
        {
            case "/start":
                await ResetToInitial(botId, chatId, cancellationToken);
                return;

            case "/create-note" or MenuButtons.CreateNote:
                await DispatchIfAllowed(
                    botId, chatId, state, () => new PreviewNoteCommand(botId, message), cancellationToken);
                return;

            case "/confirm-note" or MenuButtons.ConfirmNote:
                await DispatchIfAllowed(
                    botId, chatId, state, () => new ConfirmNoteCommand(botId, message), cancellationToken);
                return;

            case "/save-to-github" or MenuButtons.SaveToGitHub:
                await DispatchIfAllowed(
                    botId, chatId, state, () => new PushNoteToGitHubCommand(botId, message), cancellationToken);
                return;

            case "/connect-github" or MenuButtons.ConnectGitHub:
                await StartGitHubConnectFlow(botId, chatId, cancellationToken);
                return;

            case "/add-tag" or MenuButtons.AddTag:
                await StartAddTagFlow(botId, chatId, cancellationToken);
                return;

            case "/list-tags" or MenuButtons.ListTags:
                await DispatchIfAllowed(
                    botId, chatId, state, () => new ListTagsCommand(botId, message), cancellationToken);
                return;

            case MenuButtons.EditText:
                await StartEditFlow(botId, chatId, state, cancellationToken);
                return;

            case MenuButtons.Cancel when state == ChatState.AwaitingTagName:
                await CancelAddTagFlow(botId, chatId, cancellationToken);
                return;

            case MenuButtons.Cancel when state == ChatState.PreviewingNote:
                await CancelPreviewFlow(botId, chatId, cancellationToken);
                return;

            case MenuButtons.Cancel:
                await CancelEditFlow(botId, chatId, state, cancellationToken);
                return;

            case MenuButtons.SendText:
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: state == ChatState.AwaitingEditPrompt
                        ? "Type a message describing how to change the transcription."
                        : "Type your question and send it as a message.",
                    replyMarkup: MenuKeyboardFactory.Build(state),
                    cancellationToken: cancellationToken);
                return;

            case MenuButtons.SendVoice:
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "Record and send a voice message.",
                    replyMarkup: MenuKeyboardFactory.Build(state),
                    cancellationToken: cancellationToken);
                return;
        }

        if (message.Text.StartsWith('/'))
        {
            return;
        }

        await HandleText(botId, state, message, cancellationToken);
    }

    private async Task HandleText(long botId, ChatState state, Message message, CancellationToken cancellationToken)
    {
        if (state == ChatState.AwaitingEditPrompt)
        {
            await DispatchIfAllowed(
                botId, message.Chat.Id, state,
                () => new EditTranscriptionCommand(botId, message, message.Text), cancellationToken);
            return;
        }

        if (state == ChatState.AwaitingTagName)
        {
            await DispatchIfAllowed(
                botId, message.Chat.Id, state, () => new AddTagCommand(botId, message), cancellationToken);
            return;
        }

        await DispatchIfAllowed(
            botId, message.Chat.Id, state, () => new ProcessTextMessageCommand(botId, message), cancellationToken);
    }

    private async Task HandleVoice(long botId, ChatState state, Message message, CancellationToken cancellationToken)
    {
        if (state == ChatState.AwaitingEditPrompt)
        {
            await DispatchIfAllowed(
                botId, message.Chat.Id, state,
                () => new EditTranscriptionCommand(botId, message, TextPrompt: null), cancellationToken);
            return;
        }

        await DispatchIfAllowed(
            botId, message.Chat.Id, state, () => new ProcessVoiceMessageCommand(botId, message), cancellationToken);
    }

    private async Task DispatchIfAllowed<TCommand>(
        long botId,
        long chatId,
        ChatState state,
        Func<TCommand> commandFactory,
        CancellationToken cancellationToken)
        where TCommand : class
    {
        if (!ChatStateCommandsMap.IsAllowed<TCommand>(state))
        {
            logger.LogInformation(
                "Command {Command} is not allowed in state {State} for bot {BotId} chat {ChatId}",
                typeof(TCommand).Name, state, botId, chatId);

            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "This action is not available right now. Pick one from the menu below.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: cancellationToken);
            return;
        }

        await sendEndpointProvider.Send(commandFactory(), cancellationToken);
    }

    private async Task StartEditFlow(long botId, long chatId, ChatState state, CancellationToken cancellationToken)
    {
        if (state is not (ChatState.HasTranscription or ChatState.PreviewingNote))
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "There is no transcription to edit. Send a voice message first.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: cancellationToken);
            return;
        }

        // Editing the transcription invalidates any pending preview; it will be regenerated on the next preview.
        pendingNoteStore.Clear(botId, chatId);
        await chatStateStore.SetAsync(botId, chatId, ChatState.AwaitingEditPrompt, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Send a text or voice message describing how you want to change the transcription.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingEditPrompt),
            cancellationToken: cancellationToken);
    }

    private async Task CancelEditFlow(long botId, long chatId, ChatState state, CancellationToken cancellationToken)
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

        await chatStateStore.SetAsync(botId, chatId, ChatState.HasTranscription, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Edit cancelled.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.HasTranscription),
            cancellationToken: cancellationToken);
    }

    private async Task CancelPreviewFlow(long botId, long chatId, CancellationToken cancellationToken)
    {
        pendingNoteStore.Clear(botId, chatId);
        await chatStateStore.SetAsync(botId, chatId, ChatState.HasTranscription, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Note discarded. Your transcription is still here.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.HasTranscription),
            cancellationToken: cancellationToken);
    }

    private async Task StartAddTagFlow(long botId, long chatId, CancellationToken cancellationToken)
    {
        await chatStateStore.SetAsync(botId, chatId, ChatState.AwaitingTagName, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Send the name of the tag you want to add.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingTagName),
            cancellationToken: cancellationToken);
    }

    private async Task CancelAddTagFlow(long botId, long chatId, CancellationToken cancellationToken)
    {
        await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Tag creation cancelled.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: cancellationToken);
    }

    private async Task ResetToInitial(long botId, long chatId, CancellationToken cancellationToken)
    {
        await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Welcome! Send a text question or a voice message to get started.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: cancellationToken);
    }

    private async Task StartGitHubConnectFlow(long botId, long chatId, CancellationToken cancellationToken)
    {
        pendingGitHubLinkStore.Clear(botId, chatId);
        await chatStateStore.SetAsync(botId, chatId, ChatState.AwaitingGitHubRepo, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Let's connect a GitHub repository for your notes.\n\n"
                  + "Send the repository URL, for example:\nhttps://github.com/owner/repo",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingGitHubRepo),
            cancellationToken: cancellationToken);
    }

    private async Task HandleGitHubOnboarding(long botId, ChatState state, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var text = message.Text;

        if (text is "/start" or MenuButtons.Cancel)
        {
            pendingGitHubLinkStore.Clear(botId, chatId);

            if (text == "/start")
            {
                await ResetToInitial(botId, chatId, cancellationToken);
                return;
            }

            await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, cancellationToken);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "GitHub setup cancelled.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "Please send the requested value as a text message, or tap Cancel.",
                replyMarkup: MenuKeyboardFactory.Build(state),
                cancellationToken: cancellationToken);
            return;
        }

        if (state == ChatState.AwaitingGitHubRepo)
        {
            await CaptureGitHubRepo(botId, chatId, text, cancellationToken);
            return;
        }

        await CaptureGitHubToken(botId, message, chatId, text, cancellationToken);
    }

    private async Task CaptureGitHubRepo(long botId, long chatId, string repoInput, CancellationToken cancellationToken)
    {
        pendingGitHubLinkStore.SetRepo(botId, chatId, repoInput.Trim());
        await chatStateStore.SetAsync(botId, chatId, ChatState.AwaitingGitHubToken, cancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Now send a GitHub personal access token with \"Contents: Read and write\" permission on that repository.\n\n"
                  + "Use a fine-grained token scoped to just this repo. I'll delete your token message right after reading it, "
                  + "and you can revoke the token anytime in GitHub settings.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.AwaitingGitHubToken),
            cancellationToken: cancellationToken);
    }

    private async Task CaptureGitHubToken(long botId, Message message, long chatId, string token, CancellationToken cancellationToken)
    {
        var repoInput = pendingGitHubLinkStore.GetRepo(botId, chatId);
        if (repoInput is null)
        {
            await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, cancellationToken);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "The GitHub setup expired. Please start again with /connect-github.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: cancellationToken);
            return;
        }

        await DeleteTokenMessageSafe(chatId, message.MessageId, cancellationToken);
        pendingGitHubLinkStore.Clear(botId, chatId);

        await DispatchIfAllowed(
            botId, chatId, ChatState.AwaitingGitHubToken,
            () => new ConnectGitHubCommand(botId, message, repoInput, token.Trim()), cancellationToken);
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