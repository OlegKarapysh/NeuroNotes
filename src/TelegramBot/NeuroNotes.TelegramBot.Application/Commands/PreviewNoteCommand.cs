namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Generates a Markdown note from the user's last transcription and shows it as a preview
/// <b>without saving</b>. The user then confirms (<see cref="ConfirmNoteCommand"/>), edits, or cancels.
/// </summary>
public sealed record PreviewNoteCommand(long BotId, Message Message) : IBotScopedMessage;

public sealed class PreviewNoteCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteService noteService,
    ILastTranscriptionStore lastTranscriptionStore,
    IPendingNoteStore pendingNoteStore,
    IChatStateStore chatStateStore) : IConsumer<PreviewNoteCommand>
{
    public async Task Consume(ConsumeContext<PreviewNoteCommand> context)
    {
        var botId = context.Message.BotId;
        var chatId = context.Message.Message.Chat.Id;

        var lastTranscription = await lastTranscriptionStore.GetAsync(botId, chatId, context.CancellationToken);
        if (lastTranscription is null)
        {
            await chatStateStore.SetAsync(botId, chatId, ChatState.Initial, context.CancellationToken);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "No transcription found. Please send a voice message first",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        await telegramBotClient.SendChatAction(chatId, ChatAction.Typing, cancellationToken: context.CancellationToken);

        var noteResult = await noteService.GenerateNote(botId, chatId, lastTranscription, context.CancellationToken);
        if (noteResult.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: noteResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(botId, chatId, context.CancellationToken)),
                cancellationToken: context.CancellationToken);
            return;
        }

        var note = noteResult.Value;
        pendingNoteStore.Set(botId, chatId, note);
        await chatStateStore.SetAsync(botId, chatId, ChatState.PreviewingNote, context.CancellationToken);

        // Raw Markdown (incl. YAML front matter) as plain text so the user sees exactly what will be saved;
        // the Confirm / Edit / Cancel actions live on the reply keyboard.
        var preview = $"📄 Preview of your note — review it, then choose an action below:\n\n{note.Markdown}";

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: preview,
            replyMarkup: MenuKeyboardFactory.Build(ChatState.PreviewingNote),
            cancellationToken: context.CancellationToken);
    }
}