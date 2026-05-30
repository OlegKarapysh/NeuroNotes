using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.PromptEvaluation.Configuration;
using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Loading;

/// <summary>
/// Loads test cases from a flat directory. Each case is a pair of files sharing a base name:
/// <c>my-case.ogg</c> (audio) + <c>my-case.txt</c> (reference transcription).
/// </summary>
public sealed class DirectoryTestCaseLoader(
    IOptions<PromptEvaluationOptions> options,
    ILogger<DirectoryTestCaseLoader> logger) : ITestCaseLoader
{
    private static readonly string[] AudioExtensions = [".ogg", ".oga", ".opus", ".wav", ".mp3", ".m4a"];

    private readonly PromptEvaluationOptions _options = options.Value;

    public async Task<IReadOnlyList<TranscriptionTestCase>> Load(CancellationToken cancellationToken = default)
    {
        var directory = ResolvePath(_options.TestCasesDirectory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Test cases directory not found: {directory}. " +
                $"Create one and add paired files like 'sample.ogg' + 'sample.txt'.");
        }

        var audioFiles = Directory.EnumerateFiles(directory)
            .Where(path => AudioExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cases = new List<TranscriptionTestCase>(audioFiles.Count);

        foreach (var audioPath in audioFiles)
        {
            var name = Path.GetFileNameWithoutExtension(audioPath);
            var referencePath = Path.Combine(directory, name + ".txt");

            if (!File.Exists(referencePath))
            {
                logger.LogWarning(
                    "Skipping '{Audio}': no matching reference file '{Reference}'",
                    audioPath, referencePath);
                continue;
            }

            var referenceText = (await File.ReadAllTextAsync(referencePath, cancellationToken)).Trim();
            if (referenceText.Length == 0)
            {
                logger.LogWarning("Skipping '{Audio}': reference text is empty", audioPath);
                continue;
            }

            cases.Add(new TranscriptionTestCase(name, audioPath, referenceText));
        }

        logger.LogInformation("Loaded {Count} test case(s) from '{Directory}'", cases.Count, directory);
        return cases;
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
}
