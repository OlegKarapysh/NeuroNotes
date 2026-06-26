using System.Diagnostics;
using System.Globalization;
using NeuroNotes.AudioProcessing.Application.Interfaces;

namespace NeuroNotes.WebApi.LoadTest;

/// <summary>
/// Isolates the cost of the <b>Whisper transcription step alone</b>. It converts the sample to WAV
/// once (so FFmpeg is excluded) and warms the model (so the one-time weight load + JIT are excluded),
/// then runs N transcriptions <b>sequentially</b> — one at a time, so the peak working-set delta over
/// the post-warmup baseline is a single transcription's RAM footprint, and the process CPU-time delta
/// is its CPU cost (Whisper runs its native threads in-process, so they are captured).
/// </summary>
public static class IsolationProbe
{
    public static async Task RunAsync(IServiceScopeFactory scopeFactory, LoadTestOptions options, byte[] oggAudio)
    {
        var iterations = options.TotalPerLevel;
        var inv = CultureInfo.InvariantCulture;

        Console.WriteLine();
        Console.WriteLine("=== isolation: Whisper transcription only (FFmpeg excluded, model pre-loaded) ===");

        // Convert to WAV once — the FFmpeg cost is outside every measurement below.
        var wav = await ConvertToWavAsync(scopeFactory, oggAudio);

        // Warm the model (first call loads ggml-base into RAM) and JIT the path.
        for (var i = 0; i < Math.Max(1, options.Warmup); i++)
        {
            await RecognizeOnceAsync(scopeFactory, wav);
        }

        // Baseline with the model resident and warmup garbage collected.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var baselineWorkingSet = process.WorkingSet64;
        var baselineCpu = process.TotalProcessorTime;
        var baselineManaged = GC.GetTotalMemory(forceFullCollection: true);

        // Sample the working set finely so we catch the in-call peak, not just the post-call value.
        using var sampler = new ResourceSampler(50);
        sampler.Start();
        var wall = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            await RecognizeOnceAsync(scopeFactory, wav);
        }

        wall.Stop();
        var resources = await sampler.StopAsync();
        process.Refresh();

        var cpu = process.TotalProcessorTime - baselineCpu;
        var managedDelta = GC.GetTotalMemory(forceFullCollection: false) - baselineManaged;
        var peakRssDelta = resources.MaxProcessWorkingSetBytes - baselineWorkingSet;

        const double mb = 1024.0 * 1024.0;
        Console.WriteLine(string.Create(inv, $"iterations            : {iterations} sequential"));
        Console.WriteLine(string.Create(inv, $"WAV fed to Whisper    : {wav.Length / 1024.0:F0} KB"));
        Console.WriteLine();
        Console.WriteLine(string.Create(inv, $"wall / transcription  : {wall.Elapsed.TotalMilliseconds / iterations:F0} ms"));
        Console.WriteLine(string.Create(inv, $"CPU  / transcription  : {cpu.TotalMilliseconds / iterations:F0} ms of core time"));
        Console.WriteLine(string.Create(inv, $"avg cores used        : {cpu.TotalMilliseconds / wall.Elapsed.TotalMilliseconds:F2}  (CPU time ÷ wall time)"));
        Console.WriteLine();
        Console.WriteLine(string.Create(inv, $"baseline RSS (model loaded) : {baselineWorkingSet / mb:F0} MB"));
        Console.WriteLine(string.Create(inv, $"peak RSS during run         : {resources.MaxProcessWorkingSetBytes / mb:F0} MB"));
        Console.WriteLine(string.Create(inv, $"=> RAM per transcription    : {peakRssDelta / mb:F0} MB (peak over baseline)"));
        Console.WriteLine(string.Create(inv, $"   managed-heap delta       : {managedDelta / mb:F1} MB (the rest is native Whisper state)"));
        Console.WriteLine();
        Console.WriteLine("FFmpeg is excluded (separate child process); measure it with OS tools (pidstat / docker stats).");
        Console.WriteLine("Run on the droplet for real numbers — 'avg cores used' caps at 1.0 on its single vCPU.");
    }

    private static async Task RecognizeOnceAsync(IServiceScopeFactory scopeFactory, byte[] wav)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var recognizer = scope.ServiceProvider.GetRequiredService<ISpeechRecognizer>();
        using var wavStream = new MemoryStream(wav.Length);
        await wavStream.WriteAsync(wav);
        wavStream.Position = 0;
        await recognizer.RecognizeSpeech(wavStream);
    }

    private static async Task<byte[]> ConvertToWavAsync(IServiceScopeFactory scopeFactory, byte[] ogg)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var converter = scope.ServiceProvider.GetRequiredService<IAudioConverter>();
        using var oggStream = new MemoryStream(ogg.Length);
        await oggStream.WriteAsync(ogg);
        oggStream.Position = 0;

        var result = await converter.ConvertOggToWav(oggStream);
        if (result.IsFailed)
        {
            throw new InvalidOperationException($"OGG→WAV conversion failed: {result.Errors.First().Message}");
        }

        await using var wavStream = result.Value;
        using var memory = new MemoryStream();
        await wavStream.CopyToAsync(memory);
        return memory.ToArray();
    }
}