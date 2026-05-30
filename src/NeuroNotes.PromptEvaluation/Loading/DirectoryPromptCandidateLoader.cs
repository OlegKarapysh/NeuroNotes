using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.PromptEvaluation.Configuration;
using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Loading;

/// <summary>
/// Loads prompt candidates from a directory. Each <c>*.txt</c> file is one candidate;
/// the file name (without extension) becomes the prompt's display name.
/// </summary>
public sealed class DirectoryPromptCandidateLoader(
    IOptions<PromptEvaluationOptions> options,
    ILogger<DirectoryPromptCandidateLoader> logger) : IPromptCandidateLoader
{
    private readonly PromptEvaluationOptions _options = options.Value;

    public async Task<IReadOnlyList<PromptCandidate>> Load(CancellationToken cancellationToken = default)
    {
        var directory = ResolvePath(_options.PromptsDirectory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Prompts directory not found: {directory}. " +
                $"Create one and add one or more '*.txt' files, each containing a candidate system prompt.");
        }

        var files = Directory.EnumerateFiles(directory, "*.txt")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var candidates = new List<PromptCandidate>(files.Count);

        foreach (var file in files)
        {
            var prompt = (await File.ReadAllTextAsync(file, cancellationToken)).Trim();
            if (prompt.Length == 0)
            {
                logger.LogWarning("Skipping empty prompt file '{File}'", file);
                continue;
            }

            candidates.Add(new PromptCandidate(Path.GetFileNameWithoutExtension(file), prompt));
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No prompt candidates found in '{directory}'.");
        }

        logger.LogInformation("Loaded {Count} prompt candidate(s) from '{Directory}'", candidates.Count, directory);
        return candidates;
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
}
