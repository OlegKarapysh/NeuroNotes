namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Turns the user's last transcription into a Markdown note and commits it to their linked GitHub repository.
/// </summary>
public sealed record PushNoteToGitHubCommand(Message Message);

public sealed class PushNoteToGitHubCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteService noteService,
    IGitHubNotePublisher gitHubNotePublisher,
    IUserGitHubSettingsStore userGitHubSettingsStore,
    ILastTranscriptionStore lastTranscriptionStore,
    IChatStateStore chatStateStore) : IConsumer<PushNoteToGitHubCommand>
{
    public async Task Consume(ConsumeContext<PushNoteToGitHubCommand> context)
    {
        var chatId = context.Message.Message.Chat.Id;

        var lastTranscription = await lastTranscriptionStore.GetAsync(chatId, context.CancellationToken);
        if (lastTranscription is null)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "No transcription found. Please send a voice message first.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        var settings = await userGitHubSettingsStore.GetAsync(chatId, context.CancellationToken);
        if (settings is null)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "You haven't connected a GitHub repository yet. Use /connect-github to link one.",
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(chatId, context.CancellationToken)),
                cancellationToken: context.CancellationToken);
            return;
        }

        await telegramBotClient.SendChatAction(chatId, ChatAction.UploadDocument, cancellationToken: context.CancellationToken);

        var noteResult = await noteService.GenerateNote(chatId, lastTranscription, context.CancellationToken);
        if (noteResult.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: noteResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(chatId, context.CancellationToken)),
                cancellationToken: context.CancellationToken);
            return;
        }

        var createdNote = noteResult.Value;
        await noteService.SaveNote(chatId, createdNote, context.CancellationToken);

        var publishResult = await gitHubNotePublisher.PublishNote(
            settings, createdNote.FileName, createdNote.Markdown, context.CancellationToken);
        if (publishResult.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: publishResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(chatId, context.CancellationToken)),
                cancellationToken: context.CancellationToken);
            return;
        }

        await chatStateStore.SetAsync(chatId, ChatState.Initial, context.CancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: $"✅ Saved to {settings.Owner}/{settings.Repo}:\n{publishResult.Value.FileUrl}",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: context.CancellationToken);
    }
}