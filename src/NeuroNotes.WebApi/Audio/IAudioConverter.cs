namespace NeuroNotes.WebApi.Audio;

public interface IAudioConverter
{
    Task<byte[]> ConvertOggToWavAsync(ReadOnlyMemory<byte> oggData, CancellationToken cancellationToken = default);
}
