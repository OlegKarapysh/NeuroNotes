namespace NeuroNotes.WebApi.AudioConversion;

public sealed class FFmpegProcessAudioConverter(
    IOptions<AudioConversionOptions> audioConversionOptions, ILogger<FFmpegProcessAudioConverter> logger) : IAudioConverter
{
    public async Task<Stream> ConvertOggToWav(MemoryStream oggData, CancellationToken cancellationToken = default)
    {
        var options = audioConversionOptions.Value;
        
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        var timeoutCt = timeoutCts.Token;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = options.FFmpegPath,
            Arguments = "-hide_banner -loglevel error -i pipe:0 -ar 16000 -ac 1 -sample_fmt s16 -f wav pipe:1",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        try
        {
            var writeTask = WriteStdinAsync(process, oggData, timeoutCt);
            var readStdoutTask = ReadStdoutAsync(process, timeoutCt);
            var readStderrTask = process.StandardError.ReadToEndAsync(timeoutCt);

            await Task.WhenAll(writeTask, readStdoutTask, readStderrTask);
            await process.WaitForExitAsync(timeoutCt);

            if (process.ExitCode != 0)
            {
                var stderr = await readStderrTask;
                logger.LogError("FFmpeg failed with exit code {ExitCode}: {Error}", process.ExitCode, stderr);
                throw new FFmpegException(process.ExitCode, stderr);
            }

            return await readStdoutTask;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("FFmpeg timed out after {Timeout}s", options.TimeoutSeconds);
            Kill(process);
            throw new FFmpegException(-1, $"FFmpeg timed out after {options.TimeoutSeconds} seconds");
        }
        catch
        {
            // Avoid orphaned processes
            Kill(process);
            throw;
        }
    }

    private static async Task WriteStdinAsync(Process process, MemoryStream data, CancellationToken ct)
    {
        await using var stdin = process.StandardInput.BaseStream;
        await stdin.WriteAsync(data.GetBuffer().AsMemory(0, (int)data.Length), ct);
    }

    private static async Task<Stream> ReadStdoutAsync(Process process, CancellationToken ct)
    {
        var ms = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
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