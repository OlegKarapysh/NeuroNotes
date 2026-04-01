namespace NeuroNotes.WebApi.Audio;

public class FFmpegException : Exception
{
    public int ExitCode { get; }
    public string FFmpegError { get; }

    public FFmpegException(int exitCode, string ffmpegError)
        : base($"FFmpeg exited with code {exitCode}: {ffmpegError}")
    {
        ExitCode = exitCode;
        FFmpegError = ffmpegError;
    }
}
