namespace NeuroNotes.WebApi.Audio;

public interface IAudioConverter
{
    Task<byte[]> ConvertOggToWav(ReadOnlyMemory<byte> oggData, CancellationToken cancellationToken = default);
}
