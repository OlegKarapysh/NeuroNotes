using System.Text;
using NeuroNotes.AiAssistant.Public.Interfaces;
using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record CreateNoteCommand(Message Message);

public sealed class CreateNoteCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteService noteService,
    ITagStore tagStore,
    ITagSuggester tagSuggester,
    ILastTranscriptionStore lastTranscriptionStore,
    IChatStateStore chatStateStore) : IConsumer<CreateNoteCommand>
{
    public async Task Consume(ConsumeContext<CreateNoteCommand> context)
    {
        var chatId = context.Message.Message.Chat.Id;

        var lastTranscription = lastTranscriptionStore.Get(chatId);
        if (lastTranscription is null)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "No transcription found. Please send a voice message first",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        await telegramBotClient.SendChatAction(chatId, ChatAction.UploadDocument, cancellationToken: context.CancellationToken);

        var noteResult = await noteService.CreateNote(chatId, lastTranscription, context.CancellationToken);
        if (noteResult.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: noteResult.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(chatStateStore.Get(chatId)),
                cancellationToken: context.CancellationToken);
            return;
        }

        var createdNote = noteResult.Value;

        await using var noteStream = new MemoryStream(Encoding.UTF8.GetBytes(createdNote.Markdown));

        await telegramBotClient.SendDocument(
            chatId: chatId,
            document: InputFile.FromStream(noteStream, fileName: createdNote.FileName),
            cancellationToken: context.CancellationToken);

        chatStateStore.Set(chatId, ChatState.Initial);

        var message = new StringBuilder("Note created.");

        var suggestedTags = await SuggestTags(chatId, createdNote.Markdown, context.CancellationToken);
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