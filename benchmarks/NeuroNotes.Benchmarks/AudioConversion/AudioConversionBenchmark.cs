using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.WebApi.AudioConversion;
using NeuroNotes.WebApi.Configurations;

namespace NeuroNotes.Benchmarks.AudioConversion;

[MemoryDiagnoser]
public class AudioConversionBenchmark
{
    private const string SampleOggFileName = "AudioConversion/sample-voice.ogg";
    private byte[]? _sampleOggFileData;
    private MemoryStream? _memoryStream;
    private FFmpegProcessAudioConverter _ffmpegProcessAudioConverter = new FFmpegProcessAudioConverter(
        audioConversionOptions: new OptionsWrapper<AudioConversionOptions>(new AudioConversionOptions
        {
            FFmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe",
            TimeoutSeconds = 30
        }),
        logger: new Logger<FFmpegProcessAudioConverter>(LoggerFactory.Create(c => c.SetMinimumLevel(LogLevel.Warning))));

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _sampleOggFileData = await File.ReadAllBytesAsync(path: SampleOggFileName);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _memoryStream = new MemoryStream(
            buffer: _sampleOggFileData!,
            index: 0,
            count: _sampleOggFileData!.Length,
            writable: false,
            publiclyVisible: true);
    }

    [Benchmark]
    public async Task ConvertOggToWav()
    {
        using (_memoryStream)
        {
            await using var stream = await _ffmpegProcessAudioConverter.ConvertOggToWav(_memoryStream!);
        }
    }
}