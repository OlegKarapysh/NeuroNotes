namespace NeuroNotes.TelegramBot.Application.Services;

/// <summary>
/// Holds the note generated for a chat while it is being previewed, between the preview step and the
/// user's confirmation. Transient scratch state — like <see cref="IPendingGitHubLinkStore"/>, it is fine
/// to lose on restart (confirmation regenerates from the stored transcription as a fallback).
/// </summary>
public interface IPendingNoteStore
{
    void Set(long chatId, CreatedNote note);
    CreatedNote? Get(long chatId);
    void Clear(long chatId);
}