using NeuroNotes.AiAssistant.Public.Interfaces;
using NeuroNotes.TelegramBot.Application.Menus;

namespace NeuroNotes.TelegramBot.Application.Commands;

/// <summary>
/// Edits the user's last stored transcription using an LLM prompt.
/// If <see cref="TextPrompt"/> is provided, it is used directly; otherwise the
/// voice attached to <see cref="Message"/> is transcribed and used as the prompt.
/// </summary>
public sealed record EditTranscriptionCommand(Message Message, string? TextPrompt);

public sealed class EditTranscriptionCommandHandler(
    ITelegramBotClient telegramBotClient,
    IVoiceEnhanceTranscriber voiceTranscriber,
    INoteTextEditor noteTextEditor,
    ILastTranscriptionStore lastTranscriptionStore,
    IChatStateStore chatStateStore) : IConsumer<EditTranscriptionCommand>
{
    public async Task Consume(ConsumeContext<EditTranscriptionCommand> context)
    {
        var message = context.Message.Message;
        var chatId = message.Chat.Id;

        var currentText = await lastTranscriptionStore.GetAsync(chatId, context.CancellationToken);
        if (currentText is null)
        {
            await chatStateStore.SetAsync(chatId, ChatState.Initial, context.CancellationToken);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "Nothing to edit. Please send a voice message first.",
                replyMarkup: MenuKeyboardFactory.Build(ChatState.Initial),
                cancellationToken: context.CancellationToken);
            return;
        }

        await telegramBotClient.SendChatAction(
            chatId: chatId,
            action: ChatAction.Typing,
            cancellationToken: context.CancellationToken);

        var promptResult = await ResolvePrompt(context.Message, context.CancellationToken);
        if (promptResult.Prompt is null)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: promptResult.ErrorMessage ?? "Could not read the edit prompt.",
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(chatId, context.CancellationToken)),
                cancellationToken: context.CancellationToken);
            return;
        }

        var edited = await noteTextEditor.EditText(currentText, promptResult.Prompt, context.CancellationToken);
        if (edited.IsFailed)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: edited.Errors.First().Message,
                replyMarkup: MenuKeyboardFactory.Build(await chatStateStore.GetAsync(chatId, context.CancellationToken)),
                cancellationToken: context.CancellationToken);
            return;
        }

        await lastTranscriptionStore.SaveAsync(chatId, edited.Value, context.CancellationToken);
        await chatStateStore.SetAsync(chatId, ChatState.HasTranscription, context.CancellationToken);

        await telegramBotClient.SendMessage(
            chatId: chatId,
            text: edited.Value,
            replyMarkup: MenuKeyboardFactory.Build(ChatState.HasTranscription),
            cancellationToken: context.CancellationToken);
    }

    private async Task<PromptResolution> ResolvePrompt(
        EditTranscriptionCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.TextPrompt))
        {
            return new PromptResolution(command.TextPrompt, null);
        }

        var voice = command.Message.Voice;
        if (voice is null)
        {
            return new PromptResolution(null, "No prompt provided.");
        }

        var filePath = (await telegramBotClient.GetFile(voice.FileId, cancellationToken)).FilePath
                       ?? throw new InvalidOperationException("Voice message file path is missing");

        using var memoryStream = new MemoryStream();
        await telegramBotClient.DownloadFile(filePath, memoryStream, cancellationToken);

        var transcribed = await voiceTranscriber.Transcribe(memoryStream);
        if (transcribed.IsFailed)
        {
            return new PromptResolution(null, transcribed.Errors.First().Message);
        }

        return new PromptResolution(transcribed.Value, null);
    }

    private readonly record struct PromptResolution(string? Prompt, string? ErrorMessage);
}