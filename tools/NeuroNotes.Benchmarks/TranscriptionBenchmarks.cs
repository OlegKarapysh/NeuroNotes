using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NeuroNotes.AudioProcessing.Application.Interfaces;
using NeuroNotes.AudioProcessing.Infrastructure.AudioConversion;
using NeuroNotes.AudioProcessing.Infrastructure.SpeechRecognition;

namespace NeuroNotes.Benchmarks;

/// <summary>
/// Benchmarks the local voice pipeline with the real production classes. Use this for the CPU /
/// latency / managed-allocation axis — it is rigorous about time and GC, but note that
/// <see cref="MemoryDiagnoserAttribute"/> only measures the MANAGED heap. Whisper's real footprint is
/// native (whisper_state) and FFmpeg's is a separate process, so the "Allocated" column UNDERCOUNTS
/// the true RAM — measure that with the loadtest IsolationProbe or OS tools (docker stats / time -v).
///
/// Monitoring strategy: transcription takes seconds, so we measure real runs instead of BenchmarkDotNet's
/// default high-invocation pilot. Provide a real voice note via the SAMPLE_OGG env var.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 10)]
public class TranscriptionBenchmarks
{
    private WhisperProcessorFactory _processorFactory = null!;
    private ISpeechRecognizer _recognizer = null!;
    private IAudioConverter _converter = null!;
    private byte[] _ogg = null!;
    private byte[] _wav = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        var modelPath = Environment.GetEnvironmentVariable("WHISPER_MODEL")
                        ?? Path.Combine(AppContext.BaseDirectory, "ggml-base.bin");
        _processorFactory = new WhisperProcessorFactory(
            Options.Create(new SpeechRecognitionOptions { ModelFileName = modelPath }));
        _recognizer = new WhisperSpeechRecognizer(_processorFactory);

        var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";
        _converter = new FFmpegAudioConverter(
            Options.Create(new AudioConversionOptions { FFmpegPath = ffmpegPath, TimeoutSeconds = 30 }),
            NullLogger<FFmpegAudioConverter>.Instance);

        var samplePath = Environment.GetEnvironmentVariable("SAMPLE_OGG")
                         ?? throw new InvalidOperationException(
                             "Set the SAMPLE_OGG env var to a real .ogg/.oga voice note (absolute path).");
        _ogg = await File.ReadAllBytesAsync(samplePath);

        // Pre-convert once so the Transcribe benchmark measures Whisper only (no FFmpeg).
        _wav = await ConvertToWavAsync();
    }

    [GlobalCleanup]
    public void Cleanup() => _processorFactory.Dispose();

    /// <summary>Whisper transcription only (WAV already prepared) — the headline CPU/latency number.</summary>
    [Benchmark(Description = "Whisper transcription (no FFmpeg)")]
    public async Task<string?> Transcribe()
    {
        using var stream = new MemoryStream(_wav.Length);
        await stream.WriteAsync(_wav);
        stream.Position = 0;
        var result = await _recognizer.RecognizeSpeech(stream);
        return result.ValueOrDefault;
    }

    /// <summary>FFmpeg OGG→WAV conversion only (managed allocs; native ffmpeg cost is out-of-process).</summary>
    [Benchmark(Description = "FFmpeg OGG->WAV")]
    public async Task<int> Convert() => (await ConvertToWavAsync()).Length;

    private async Task<byte[]> ConvertToWavAsync()
    {
        using var ogg = new MemoryStream(_ogg.Length);
        await ogg.WriteAsync(_ogg);
        ogg.Position = 0;

        var result = await _converter.ConvertOggToWav(ogg);
        if (result.IsFailed)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        await using var wav = result.Value;
        using var memory = new MemoryStream();
        await wav.CopyToAsync(memory);
        return memory.ToArray();
    }
}
