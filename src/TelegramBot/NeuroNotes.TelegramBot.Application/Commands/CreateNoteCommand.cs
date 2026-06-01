using NeuroNotes.AiAssistant.Public.Interfaces;
using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application.Commands;

public sealed record CreateNoteCommand(Message Message);

public sealed class CreateNoteCommandHandler(
    ITelegramBotClient telegramBotClient,
    INoteService noteService,
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

        await using var noteStream = noteResult.Value;
        noteStream.Position = 0;

        await telegramBotClient.SendDocument(
            chatId: chatId,
            document: InputFile.FromStream(noteStream, fileName: $"note_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md"),
            cancellationToken: context.CancellationToken);

        chatStateStore.Set(chatId, ChatState.Initial);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: "Note created. What would you like to do next?",
            replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
            cancellationToken: context.CancellationToken);
    }
}