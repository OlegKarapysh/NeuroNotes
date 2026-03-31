using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace NeuroNotes.WebApi.Audio;

public sealed class FFmpegAudioConverter : IAudioConverter
{
    private readonly AudioConversionOptions _options;
    private readonly ILogger<FFmpegAudioConverter> _logger;

    public FFmpegAudioConverter(IOptions<AudioConversionOptions> options, ILogger<FFmpegAudioConverter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> ConvertOggToWavAsync(
        ReadOnlyMemory<byte> oggData,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var ct = timeoutCts.Token;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _options.FFmpegPath,
            Arguments = "-hide_banner -loglevel error -i pipe:0 -ar 16000 -ac 1 -sample_fmt s16 -f wav pipe:1",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();

        try
        {
            // Run three concurrent tasks to avoid stdin/stdout deadlock:
            // 1. Write OGG data to stdin, then close it to signal EOF
            // 2. Read WAV binary from stdout via BaseStream (not StreamReader — WAV is binary)
            // 3. Read stderr text for error reporting
            var writeTask = WriteStdinAsync(process, oggData, ct);
            var readStdoutTask = ReadStdoutAsync(process, ct);
            var readStderrTask = process.StandardError.ReadToEndAsync(ct);

            await Task.WhenAll(writeTask, readStdoutTask, readStderrTask);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var stderr = await readStderrTask;
                _logger.LogError("FFmpeg failed with exit code {ExitCode}: {Error}", process.ExitCode, stderr);
                throw new AudioConversionException(process.ExitCode, stderr);
            }

            return await readStdoutTask;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("FFmpeg timed out after {Timeout}s", _options.TimeoutSeconds);
            Kill(process);
            throw new AudioConversionException(-1, $"FFmpeg timed out after {_options.TimeoutSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            throw;
        }
    }

    private static async Task WriteStdinAsync(Process process, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        await using var stdin = process.StandardInput.BaseStream;
        await stdin.WriteAsync(data, ct);
        // Disposing stdin closes the pipe, signaling EOF to FFmpeg
    }

    private static async Task<byte[]> ReadStdoutAsync(Process process, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Process may have already exited
        }
    }
}

public sealed class AudioConversionOptions
{
    public const string SectionName = "AudioConversion";

    public string FFmpegPath { get; set; } = "ffmpeg";

    public int TimeoutSeconds { get; set; } = 30;
}
