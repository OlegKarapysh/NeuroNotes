using System.Diagnostics;
using System.Globalization;

namespace NeuroNotes.WebApi.LoadTest;

/// <summary>
/// Samples memory and CPU pressure on a background loop while one concurrency level runs.
/// The cross-platform process working set is always captured; the cgroup / <c>/proc</c> reads
/// are best-effort and only succeed on the Linux container that is the production target.
/// </summary>
public sealed class ResourceSampler(int intervalMs) : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    private long _maxProcessWorkingSetBytes;
    private long _maxContainerMemoryBytes;
    private long _minHostMemAvailableBytes = long.MaxValue;
    private double _maxLoadAvg1m;

    public void Start() => _loop = SampleLoopAsync(_cts.Token);

    public async Task<ResourceStats> StopAsync()
    {
        await _cts.CancelAsync();
        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation.
            }
        }

        return new ResourceStats(
            MaxProcessWorkingSetBytes: _maxProcessWorkingSetBytes,
            MaxContainerMemoryBytes: _maxContainerMemoryBytes > 0 ? _maxContainerMemoryBytes : null,
            MinHostMemAvailableBytes: _minHostMemAvailableBytes != long.MaxValue ? _minHostMemAvailableBytes : null,
            MaxLoadAvg1m: _maxLoadAvg1m > 0 ? _maxLoadAvg1m : null);
    }

    private async Task SampleLoopAsync(CancellationToken cancellationToken)
    {
        using var process = Process.GetCurrentProcess();
        while (!cancellationToken.IsCancellationRequested)
        {
            process.Refresh();
            _maxProcessWorkingSetBytes = Math.Max(_maxProcessWorkingSetBytes, process.WorkingSet64);

            if (OperatingSystem.IsLinux())
            {
                if (ReadCGroupMemoryBytes() is { } containerBytes)
                {
                    _maxContainerMemoryBytes = Math.Max(_maxContainerMemoryBytes, containerBytes);
                }

                if (ReadHostMemAvailableBytes() is { } hostAvailable)
                {
                    _minHostMemAvailableBytes = Math.Min(_minHostMemAvailableBytes, hostAvailable);
                }

                if (ReadLoadAvg1m() is { } load)
                {
                    _maxLoadAvg1m = Math.Max(_maxLoadAvg1m, load);
                }
            }

            try
            {
                await Task.Delay(intervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // cgroup v2 exposes a single byte count; v1 lives under a memory/ subtree.
    private static long? ReadCGroupMemoryBytes() =>
        TryReadLong("/sys/fs/cgroup/memory.current")
        ?? TryReadLong("/sys/fs/cgroup/memory/memory.usage_in_bytes");

    // /proc/meminfo reports host-wide memory inside a default container, so this tracks how close
    // the whole droplet (app + Postgres + OS) is to OOM — the real capacity ceiling on 1 GB.
    private static long? ReadHostMemAvailableBytes()
    {
        try
        {
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (!line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kb))
                {
                    return kb * 1024;
                }
            }
        }
        catch (IOException)
        {
            // Best-effort.
        }

        return null;
    }

    // On a 1 vCPU box, a 1-minute load average above 1.0 means work is queueing on the core.
    private static double? ReadLoadAvg1m()
    {
        try
        {
            var firstToken = File.ReadAllText("/proc/loadavg").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return double.TryParse(firstToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var load) ? load : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static long? TryReadLong(string path)
    {
        try
        {
            return File.Exists(path) && long.TryParse(File.ReadAllText(path).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Dispose() => _cts.Dispose();
}

/// <summary>Aggregated resource readings for one concurrency level. Linux-only fields are null elsewhere.</summary>
public sealed record ResourceStats(
    long MaxProcessWorkingSetBytes,
    long? MaxContainerMemoryBytes,
    long? MinHostMemAvailableBytes,
    double? MaxLoadAvg1m);