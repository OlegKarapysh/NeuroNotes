namespace NeuroNotes.AiAssistant.Public.Interfaces;

public interface INoteAssistant
{
    Task<Result<string>> Ask(long botId, long userId, string question, CancellationToken cancellationToken = default);
}