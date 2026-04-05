using FFmpeg.NET;

namespace NeuroNotes.WebApi.AudioConversion;

public sealed class FFmpegAudioConverter(IOptions<AudioConversionOptions> audioConversionOptions) : IAudioConverter
{
    public async Task<Stream> ConvertOggToWav(MemoryStream oggData, CancellationToken cancellationToken = default)
    {
        var ffmpeg = new Engine(audioConversionOptions.Value.FFmpegPath);

        oggData.Position = 0;
        var outputStream = await ffmpeg.ConvertAsync(
            input: new StreamInput(oggData),
            options: new ConversionOptions
            {
                ExtraArguments = "-ar 16000 -sample_fmt s16 -f wav",
            },
            cancellationToken: cancellationToken);

        return outputStream;
    }
}