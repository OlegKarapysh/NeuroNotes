using System.Globalization;

namespace NeuroNotes.WebApi.LoadTest;

public sealed record LoadTestOptions
{
    /// <summary>Concurrency levels to run, in order. A multi-value list is a sweep (find the knee).</summary>
    public IReadOnlyList<int> ConcurrencyLevels { get; init; } = [1, 10];

    /// <summary>Measured iterations per concurrency level (warmup excluded).</summary>
    public int TotalPerLevel { get; init; } = 20;

    /// <summary>Unmeasured iterations before each level — JITs the path and loads the Whisper model once.</summary>
    public int Warmup { get; init; } = 2;

    /// <summary>When set, also runs the OpenAI enhance step (real OpenAI cost). Off = droplet-only path.</summary>
    public bool Enhance { get; init; }

    /// <summary>Path to a real OGG/Opus voice note</summary>
    public string? AudioPath { get; init; }

    /// <summary>Where to write the JSON report.</summary>
    public string ReportPath { get; init; } = "loadtest-report.json";

    /// <summary>Resource-sampling interval in milliseconds.</summary>
    public int SampleIntervalMs { get; init; } = 250;

    public static LoadTestOptions Parse(string[] args)
    {
        var levels = Value("--concurrency")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ? level : 0)
            .Where(level => level > 0)
            .ToArray();

        return new LoadTestOptions
        {
            ConcurrencyLevels = levels is { Length: > 0 } ? levels : [1],
            TotalPerLevel = Math.Max(1, Int("--total", 20)),
            Warmup = Math.Max(0, Int("--warmup", 2)),
            Enhance = args.Contains("--enhance"),
            AudioPath = Value("--audio"),
            ReportPath = Value("--report") ?? "loadtest-report.json",
            SampleIntervalMs = Math.Max(50, Int("--sample-ms", 250))
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