namespace NeuroNotes.WebApi.Audio;

public sealed class AudioConversionException : Exception
{
    public int ExitCode { get; }

    public string FFmpegError { get; }

    public AudioConversionException(int exitCode, string ffmpegError)
        : base($"FFmpeg exited with code {exitCode}: {ffmpegError}")
    {
        ExitCode = exitCode;
        FFmpegError = ffmpegError;
    }
}
