namespace NeuroNotes.WebApi.SpeechRecognition;

public sealed class WhisperDownloader(IHttpClientFactory httpClientFactory, ILogger<WhisperDownloader> logger)
    : IWhisperDownloader
{
    public async Task DownloadWhisper()
    {
        try
        {
            if (!File.Exists(IWhisperDownloader.WhisperModelFileName))
            {
                logger.LogInformation("Downloading Whisper model...");
            
                var ggmlModelDownloader = new WhisperGgmlDownloader(httpClientFactory.CreateClient());
            
                await using var modelStream = await ggmlModelDownloader.GetGgmlModelAsync(GgmlType.Base);
                await using var fileWriter = File.OpenWrite(IWhisperDownloader.WhisperModelFileName);
                await modelStream.CopyToAsync(fileWriter);
            
                logger.LogInformation("Whisper model downloaded!");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while downloading Whisper model 'ggml-base.bin'");
        }
    }
}