using System.Globalization;

namespace NeuroNotes.WebApi.LoadTest;

public sealed record LoadTestOptions
{
    public static readonly IReadOnlyList<int> DefaultConcurrencyLevels = [1, 3, 10];
    public const int DefaultTotalIterationsPerLevel = 30;
    public const int DefaultWarmup = 2;
    public const bool DefaultEnhance = true;
    public const int DefaultSampleIntervalMs = 250;

    /// <summary>Concurrency levels to run, in order. A multi-value list is a sweep (find the knee).</summary>
    public IReadOnlyList<int> ConcurrencyLevels { get; init; } = DefaultConcurrencyLevels;
    /// <summary>Measured iterations per concurrency level (warmup excluded).</summary>
    public int TotalPerLevel { get; init; } = DefaultTotalIterationsPerLevel;
    /// <summary>Unmeasured iterations before each level — JITs the path and loads the Whisper model once.</summary>
    public int Warmup { get; init; } = DefaultWarmup;
    /// <summary>When set, also runs the OpenAI enhance step (real OpenAI cost). Off = droplet-only path.</summary>
    public bool Enhance { get; init; } = DefaultEnhance;
    /// <summary>Path to a real OGG/Opus voice note</summary>
    public string? AudioPath { get; init; }
    /// <summary>Where to write the JSON report.</summary>
    public string ReportPath { get; init; } = "loadtest-report.json";
    /// <summary>Resource-sampling interval in milliseconds.</summary>
    public int SampleIntervalMs { get; init; } = DefaultSampleIntervalMs;

    public static LoadTestOptions Parse(string[] args)
    {
        var levels = Value("--concurrency")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ? level : 0)
            .Where(level => level > 0)
            .ToArray();

        return new LoadTestOptions
        {
            ConcurrencyLevels = levels is { Length: > 0 } ? levels : DefaultConcurrencyLevels,
            TotalPerLevel = Math.Max(1, Int("--total", DefaultTotalIterationsPerLevel)),
            Warmup = Math.Max(0, Int("--warmup", DefaultWarmup)),
            Enhance = args.Contains("--enhance"),
            AudioPath = Value("--audio"),
            ReportPath = Value("--report") ?? "loadtest-report.json",
            SampleIntervalMs = Math.Max(50, Int("--sample-ms", DefaultSampleIntervalMs))
        };

        string? Value(string key)
        {
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        int Int(string key, int fallback) =>
            Value(key) is { } raw && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
    }
}