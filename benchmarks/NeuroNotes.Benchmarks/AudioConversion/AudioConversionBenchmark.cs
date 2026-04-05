using NeuroNotes.WebApi.AudioConversion;
using NeuroNotes.WebApi.Configurations;

namespace NeuroNotes.Benchmarks.AudioConversion;

[MemoryDiagnoser]
public class AudioConversionBenchmark
{
    private const string SampleOggFileName = "AudioConversion/sample-voice.ogg";
    private byte[]? _sampleOggFileData;
    private MemoryStream? _memoryStreamForInProcessAudioConversion;
    private MemoryStream? _memoryStreamForLibraryAudioConversion;

    private static readonly IOptions<AudioConversionOptions> _audioConversionOptions = new OptionsWrapper<AudioConversionOptions>(
        new AudioConversionOptions
        {
            FFmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe",
            TimeoutSeconds = 30
        });
    
    private readonly FFmpegProcessAudioConverter _ffmpegInProcessAudioConverter = new FFmpegProcessAudioConverter(
        audioConversionOptions: _audioConversionOptions,
        logger: new Logger<FFmpegProcessAudioConverter>(LoggerFactory.Create(c => c.AddConsole())));

    private readonly FFmpegAudioConverter _ffmpegLibraryAudioConverter = new(_audioConversionOptions);
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _sampleOggFileData = await File.ReadAllBytesAsync(path: SampleOggFileName);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _memoryStreamForInProcessAudioConversion = CreateMemoryStreamFromSampleData();
        _memoryStreamForLibraryAudioConversion = CreateMemoryStreamFromSampleData();

        MemoryStream CreateMemoryStreamFromSampleData() => new(
            buffer: _sampleOggFileData!,
            index: 0,
            count: _sampleOggFileData!.Length,
            writable: false,
            publiclyVisible: true);
    }

    [Benchmark]
    public async Task ConvertOggToWavInProcess()
    {
        using (_memoryStreamForInProcessAudioConversion)
        {
            await using var stream = await _ffmpegInProcessAudioConverter.ConvertOggToWav(_memoryStreamForInProcessAudioConversion!);
        }
    }
    
    [Benchmark]
    public async Task ConvertOggToWavLibrary()
    {
        using (_memoryStreamForLibraryAudioConversion)
        {
            await using var stream = await _ffmpegLibraryAudioConverter.ConvertOggToWav(_memoryStreamForInProcessAudioConversion!);
        }
    }
}