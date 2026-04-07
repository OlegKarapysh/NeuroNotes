namespace NeuroNotes.WebApi.AudioConversion;

public interface IAudioConverter
{
    Task<Stream> ConvertOggToWav(MemoryStream oggData, CancellationToken cancellationToken = default);
}
