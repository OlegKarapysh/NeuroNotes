namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Confirms a previewed note: persists exactly what the user reviewed (see <see cref="PreviewNoteCommand"/>),
/// sends it back as a Markdown document, and reports the tags applied to it. If the preview was lost (e.g. a
/// restart), it regenerates the note from the stored transcription as a fallback.
/// </summary>
public sealed record ConfirmNoteCommand(Message Message);

public sealed class ConfirmNoteCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteService noteService,
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

        if (note.Tags.Count > 0)
        {
            message.Append("\n\n🏷 Tags: ").Append(string.Join(", ", note.Tags));
        }

        message.Append("\n\nWhat would you like to do next?");

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: message.ToString(),
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: context.CancellationToken);
    }
}