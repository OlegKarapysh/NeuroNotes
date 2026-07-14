namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Confirms a previewed note: persists exactly what the user reviewed (see <see cref="PreviewNoteCommand"/>),
/// sends it back as a Markdown document, and suggests tags. If the preview was lost (e.g. a restart), it
/// regenerates the note from the stored transcription as a fallback.
/// </summary>
public sealed record ConfirmNoteCommand(Message Message);

public sealed class ConfirmNoteCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteService noteService,
    ITagStore tagStore,
    ITagSuggester tagSuggester,
    IPendingNoteStore pendingNoteStore,
    ILastTranscriptionStore lastTranscriptionStore,
    IChatStateStore chatStateStore) : IConsumer<ConfirmNoteCommand>
{
    public async Task Consume(ConsumeContext<ConfirmNoteCommand> context)
    {
        var chatId = context.Message.Message.Chat.Id;

        var note = pendingNoteStore.Get(chatId);
        if (note is null)
        {
            // Preview cache lost (e.g. process restart between preview and confirm): regenerate from the transcription.
            var lastTranscription = await lastTranscriptionStore.GetAsync(chatId, context.CancellationToken);
            if (lastTranscription is null)
            {
                await chatStateStore.SetAsync(chatId, ChatState.Initial, context.CancellationToken);
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "No note to save. Please send a voice message first",
                    replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                    cancellationToken: context.CancellationToken);
                return;
            }

            var regenerated = await noteService.GenerateNote(chatId, lastTranscription, context.CancellationToken);
            if (regenerated.IsFailed)
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: regenerated.Errors.First().Message,
                    replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(chatId, context.CancellationToken)),
                    cancellationToken: context.CancellationToken);
                return;
            }

            note = regenerated.Value;
        }

        await telegramBotClient.SendChatAction(chatId, ChatAction.UploadDocument, cancellationToken: context.CancellationToken);

        await noteService.SaveNote(chatId, note, context.CancellationToken);
        pendingNoteStore.Clear(chatId);

        await using var noteStream = new MemoryStream(Encoding.UTF8.GetBytes(note.Markdown));

        await telegramBotClient.SendDocument(
            chatId: chatId,
            document: InputFile.FromStream(noteStream, fileName: note.FileName),
            cancellationToken: context.CancellationToken);

        await chatStateStore.SetAsync(chatId, ChatState.Initial, context.CancellationToken);

        var message = new StringBuilder("Note created.");

        var suggestedTags = await SuggestTags(chatId, note.Markdown, context.CancellationToken);
        if (suggestedTags.Count > 0)
        {
            message.Append("\n\nSuggested tags: ").Append(string.Join(", ", suggestedTags));
        }

        message.Append("\n\nWhat would you like to do next?");

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: message.ToString(),
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: context.CancellationToken);
    }

    private async Task<IReadOnlyList<string>> SuggestTags(long chatId, string noteText, CancellationToken cancellationToken)
    {
        var availableTags = await tagStore.GetAllAsync(chatId, cancellationToken);
        if (availableTags.Count == 0)
        {
            return [];
        }

        var result = await tagSuggester.SuggestTags(noteText, availableTags, cancellationToken);

        // Tag suggestions are a nicety — never let a failure here break note creation.
        return result.IsSuccess ? result.Value : [];
    }
}