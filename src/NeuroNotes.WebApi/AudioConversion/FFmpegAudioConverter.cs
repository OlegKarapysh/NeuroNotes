namespace NeuroNotes.WebApi.AudioConversion;

public class FFmpegAudioConverter : IAudioConverter
{
    public Task<Stream> ConvertOggToWav(MemoryStream oggData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}