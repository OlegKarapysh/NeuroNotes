namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Validates a user-supplied GitHub repository and access token, and on success stores the link so
/// notes can later be committed to it.
/// </summary>
public sealed record ConnectGitHubCommand(long BotId, Message Message, string RepoInput, string AccessToken) : IBotScopedMessage;

public sealed class ConnectGitHubCommandHandler(
    ITelegramBotClient telegramBotClient,
    IGitHubAccountLinker gitHubAccountLinker,
    IUserGitHubSettingsStore userGitHubSettingsStore,
    IChatStateStore chatStateStore) : IConsumer<ConnectGitHubCommand>
{
    public async Task Consume(ConsumeContext<ConnectGitHubCommand> context)
    {
        var botId = context.Message.BotId;
        var chatId = context.Message.Message.Chat.Id;
        await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, context.CancellationToken);

        var linkResult = await gitHubAccountLinker.Link(
            context.Message.RepoInput, context.Message.AccessToken, context.CancellationToken);

        if (linkResult.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: linkResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        var settings = linkResult.Value;
        await userGitHubSettingsStore.SaveAsync(botId, chatId, settings, context.CancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: $"✅ Connected to {settings.Owner}/{settings.Repo} (branch {settings.Branch}). "
                + $"Create a note, then tap {MenuButtons.SaveToGitHub} to commit it to the {settings.NotesFolder}/ folder.",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: context.CancellationToken);
    }
}