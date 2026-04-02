namespace NeuroNotes.WebApi.AudioConversion;

public interface IAudioConverter
{
    Task<byte[]> ConvertOggToWav(ReadOnlyMemory<byte> oggData, CancellationToken cancellationToken = default);
}
