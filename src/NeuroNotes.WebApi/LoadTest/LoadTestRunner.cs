using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NeuroNotes.AudioProcessing.Infrastructure.AudioConversion;
using NeuroNotes.AudioProcessing.Public.Interfaces;

namespace NeuroNotes.WebApi.LoadTest;

/// <summary>
/// Drives the real voice pipeline (FFmpeg + Whisper, optionally + OpenAI enhance) at controlled
/// concurrency to characterise the droplet's capacity. Reuses the host's DI graph, so it exercises
/// the exact production code paths without touching Telegram or the database.
/// </summary>
public static class LoadTestRunner
{
    public static async Task RunAsync(IServiceProvider services, string[] args)
    {
        var options = LoadTestOptions.Parse(args);
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        PrintHeader(options);

        var samplePath = ResolveSamplePath(options);
        byte[] audio;
        try
        {
            audio = await File.ReadAllBytesAsync(samplePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to prepare the audio sample: {ex.Message}");
            Console.Error.WriteLine("Add LoadTest/sample.ogg, pass --audio <path.ogg>, or ensure ffmpeg is on PATH.");
            return;
        }

        PrintAudioSampleInfo(options, samplePath, audio);

        var levelResults = new List<LevelResult>();
        foreach (var concurrency in options.ConcurrencyLevels)
        {
            var result = await RunLevel(scopeFactory, options, audio, concurrency);
            levelResults.Add(result);
            PrintLevel(result);

            // Settle between levels so one level's GC / memory pressure does not bleed into the next.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(2000);
        }

        WriteReport(options, levelResults);
        PrintSummary(levelResults, options);
    }

    private static void PrintAudioSampleInfo(LoadTestOptions options, string samplePath, byte[] audio)
    {
        var source = options.AudioPath is not null
            ? $"file: {samplePath}"
            : $"bundled: {samplePath}";
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Audio sample : {audio.Length / 1024.0:F0} KB ({source})"));
    }

    private static async Task<LevelResult> RunLevel(
        IServiceScopeFactory scopeFactory, LoadTestOptions options, byte[] audio, int concurrency)
    {
        Console.WriteLine();
        Console.WriteLine($"── concurrency {concurrency} : warmup {options.Warmup}, measured {options.TotalPerLevel} ──");

        for (var i = 0; i < options.Warmup; i++)
        {
            _ = await RunOnce(scopeFactory, options, audio);
        }

        var latencies = new ConcurrentBag<double>();
        var errors = 0;
        var failedResults = 0;

        using var sampler = new ResourceSampler(options.SampleIntervalMs);
        sampler.Start();
        var wall = Stopwatch.StartNew();

        await Parallel.ForEachAsync(
            Enumerable.Range(0, options.TotalPerLevel),
            new ParallelOptions { MaxDegreeOfParallelism = concurrency },
            async (_, _) =>
            {
                var (completed, succeeded, elapsedMs) = await RunOnce(scopeFactory, options, audio);
                if (!completed)
                {
                    Interlocked.Increment(ref errors);
                    return;
                }

                latencies.Add(elapsedMs);
                if (!succeeded)
                {
                    Interlocked.Increment(ref failedResults);
                }
            });

        wall.Stop();
        var resources = await sampler.StopAsync();

        return BuildLevelResult(concurrency, [.. latencies], errors, failedResults, wall.Elapsed.TotalSeconds, resources);
    }

    private static async Task<(bool Completed, bool Succeeded, double ElapsedMs)> RunOnce(
        IServiceScopeFactory scopeFactory, LoadTestOptions options, byte[] audio)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var transcriber = options.Enhance
            ? scope.ServiceProvider.GetRequiredService<IVoiceEnhanceTranscriber>()
            : scope.ServiceProvider.GetRequiredService<IVoiceTranscriber>();

        // The converter reads GetBuffer()/Length, so build the stream the same way the bot does.
        using var stream = new MemoryStream(audio.Length);
        await stream.WriteAsync(audio);
        stream.Position = 0;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await transcriber.Transcribe(stream);
            stopwatch.Stop();
            return (Completed: true, result.IsSuccess, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.Error.WriteLine($"  iteration threw: {ex.Message}");
            return (Completed: false, Succeeded: false, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static LevelResult BuildLevelResult(
        int concurrency, double[] latencies, int errors, int failedResults, double wallSeconds, ResourceStats resources)
    {
        Array.Sort(latencies);
        var completed = latencies.Length;
        var throughputPerMinute = wallSeconds > 0 ? completed / wallSeconds * 60.0 : 0;

        return new LevelResult(
            Concurrency: concurrency,
            Completed: completed,
            Errors: errors,
            FailedResults: failedResults,
            WallSeconds: Math.Round(wallSeconds, 2),
            ThroughputPerMinute: Math.Round(throughputPerMinute, 1),
            MeanMs: Math.Round(Mean(latencies), 0),
            P50Ms: Math.Round(Percentile(latencies, 50), 0),
            P90Ms: Math.Round(Percentile(latencies, 90), 0),
            P95Ms: Math.Round(Percentile(latencies, 95), 0),
            P99Ms: Math.Round(Percentile(latencies, 99), 0),
            MinMs: Math.Round(latencies.Length > 0 ? latencies[0] : 0, 0),
            MaxMs: Math.Round(latencies.Length > 0 ? latencies[^1] : 0, 0),
            ProcessWorkingSetMaxMb: Math.Round(resources.MaxProcessWorkingSetBytes / 1024.0 / 1024.0, 1),
            ContainerMemoryMaxMb: ToMb(resources.MaxContainerMemoryBytes),
            HostMemAvailableMinMb: ToMb(resources.MinHostMemAvailableBytes),
            LoadAvg1mMax: resources.MaxLoadAvg1m);
    }

    private static string ResolveSamplePath(LoadTestOptions options)
    {
        if (options.AudioPath is not null)
        {
            return options.AudioPath;
        }

        var bundled = Path.Combine(AppContext.BaseDirectory, "LoadTest", "sample.ogg");
        return File.Exists(bundled) ? bundled : throw new ArgumentException("Could not find sample.ogg");
    }

    private static void WriteReport(LoadTestOptions options, IReadOnlyList<LevelResult> levels)
    {
        var report = new
        {
            Note = "Per-user fixed cost = $6 / max_users; max_users = throughput-at-safe-utilisation / notes_per_user_per_day. See LoadTest/README.md.",
            options.Enhance,
            options.ConcurrencyLevels,
            options.TotalPerLevel,
            options.Warmup,
            Audio = options.AudioPath,
            Levels = levels
        };

        try
        {
            File.WriteAllText(options.ReportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine();
            Console.WriteLine($"Report written to {options.ReportPath}");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Could not write report to {options.ReportPath}: {ex.Message}");
        }
    }

    private static void PrintHeader(LoadTestOptions options)
    {
        Console.WriteLine();
        Console.WriteLine("=== NeuroNotes voice-pipeline load test ===");
        Console.WriteLine($"Path         : {(options.Enhance ? "FFmpeg + Whisper + OpenAI enhance (END-TO-END, INCURS OPENAI COST)" : "FFmpeg + Whisper (droplet-only, no OpenAI cost)")}");
        Console.WriteLine($"Concurrency  : {string.Join(", ", options.ConcurrencyLevels)}");
        if (options.Enhance)
        {
            Console.WriteLine("WARNING      : --enhance calls OpenAI for every iteration and will bill your account.");
        }
    }

    private static void PrintLevel(LevelResult r)
    {
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine(string.Create(inv, $"  ran {r.Completed}/{r.Completed + r.Errors}  errors {r.Errors}  non-success results {r.FailedResults}  wall {r.WallSeconds:F1}s"));
        Console.WriteLine(string.Create(inv, $"  throughput {r.ThroughputPerMinute:F1}/min  latency ms: p50 {r.P50Ms:F0}  p95 {r.P95Ms:F0}  p99 {r.P99Ms:F0}  max {r.MaxMs:F0}"));
        Console.WriteLine(string.Create(inv, $"  process RSS max {r.ProcessWorkingSetMaxMb:F0} MB") +
                          (r.ContainerMemoryMaxMb is { } c ? string.Create(inv, $"  container max {c:F0} MB") : "") +
                          (r.HostMemAvailableMinMb is { } h ? string.Create(inv, $"  host avail min {h:F0} MB") : "") +
                          (r.LoadAvg1mMax is { } l ? string.Create(inv, $"  load1m max {l:F2}") : ""));
    }

    private static void PrintSummary(IReadOnlyList<LevelResult> levels, LoadTestOptions options)
    {
        Console.WriteLine();
        Console.WriteLine("=== summary ===");
        Console.WriteLine($"{"conc",4} {"thru/min",9} {"p95 ms",8} {"errs",5} {"noRes",6} {"RSS MB",7} {"hostFreeMB",11} {"load1m",7}");
        foreach (var r in levels)
        {
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,4} {1,9:F1} {2,8:F0} {3,5} {4,6} {5,7:F0} {6,11} {7,7}",
                r.Concurrency,
                r.ThroughputPerMinute,
                r.P95Ms,
                r.Errors,
                r.FailedResults,
                r.ProcessWorkingSetMaxMb,
                r.HostMemAvailableMinMb is { } h ? h.ToString("F0", CultureInfo.InvariantCulture) : "-",
                r.LoadAvg1mMax is { } l ? l.ToString("F2", CultureInfo.InvariantCulture) : "-"));
        }

        Console.WriteLine();
        Console.WriteLine("Columns: errs = pipeline exceptions (OOM/broken process); noRes = ran but returned a failed");
        Console.WriteLine("Result (ffmpeg timeout under load — see the max latency — OR an empty transcription, which a");
        Console.WriteLine("SYNTHETIC tone produces; with a real --audio sample noRes should be ~0).");
        Console.WriteLine("Knee: the safe concurrency is the highest level with 0 errors, no timeout spikes in max latency,");
        Console.WriteLine("host-free RAM comfortably above 0, and load1m not runaway. Cap MassTransit at that number, then:");
        Console.WriteLine("  daily_capacity = (safe_throughput/min) * 60 * 24 * utilisation_factor (~0.3-0.5)");
        Console.WriteLine("  max_users      = daily_capacity / voice_notes_per_user_per_day");
        Console.WriteLine("  $/user/month   = 6 / max_users  +  OpenAI_per_user");
        if (!options.Enhance)
        {
            Console.WriteLine("(OpenAI was not exercised — this run measures droplet capacity only, which is the binding constraint.)");
        }
    }

    private static double Mean(double[] values) => values.Length == 0 ? 0 : values.Average();

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var rank = percentile / 100.0 * (sorted.Length - 1);
        var low = (int)Math.Floor(rank);
        var high = (int)Math.Ceiling(rank);
        if (low == high)
        {
            return sorted[low];
        }

        var weight = rank - low;
        return sorted[low] * (1 - weight) + sorted[high] * weight;
    }

    private static double? ToMb(long? bytes) => bytes is { } value ? Math.Round(value / 1024.0 / 1024.0, 1) : null;
}

/// <summary>One concurrency level's measured results. Serialised into the JSON report.</summary>
public sealed record LevelResult(
    int Concurrency,
    int Completed,
    int Errors,
    int FailedResults,
    double WallSeconds,
    double ThroughputPerMinute,
    double MeanMs,
    double P50Ms,
    double P90Ms,
    double P95Ms,
    double P99Ms,
    double MinMs,
    double MaxMs,
    double ProcessWorkingSetMaxMb,
    double? ContainerMemoryMaxMb,
    double? HostMemAvailableMinMb,
    double? LoadAvg1mMax);