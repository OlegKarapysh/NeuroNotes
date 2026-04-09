using FluentResults;

namespace NeuroNotes.AudioProcessing.Application.Interfaces;

public interface IAudioConverter
{
    Task<Result<Stream>> ConvertOggToWav(MemoryStream oggData, CancellationToken cancellationToken = default);
}