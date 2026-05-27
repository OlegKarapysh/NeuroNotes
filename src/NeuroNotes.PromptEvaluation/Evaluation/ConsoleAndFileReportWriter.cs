using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroNotes.PromptEvaluation.Configuration;
using NeuroNotes.PromptEvaluation.Models;

namespace NeuroNotes.PromptEvaluation.Evaluation;

/// <summary>
/// Writes a summary table to the console and, optionally, a detailed CSV report to disk.
/// </summary>
public sealed class ConsoleAndFileReportWriter(
    IOptions<PromptEvaluationOptions> options,
    ILogger<ConsoleAndFileReportWriter> logger) : IEvaluationReportWriter
{
    private readonly PromptEvaluationOptions _options = options.Value;

    public async Task Write(IReadOnlyList<PromptScore> scores, CancellationToken cancellationToken = default)
    {
        WriteConsoleSummary(scores);

        if (!string.IsNullOrWhiteSpace(_options.CsvReportPath))
        {
            await WriteCsv(scores, _options.CsvReportPath, cancellationToken);
            logger.LogInformation("Detailed CSV report written to '{Path}'", Path.GetFullPath(_options.CsvReportPath));
        }
    }

    private static void WriteConsoleSummary(IReadOnlyList<PromptScore> scores)
    {
        Console.WriteLine();
        Console.WriteLine("=== Prompt evaluation summary (best first) ===");
        Console.WriteLine($"{"Rank",-5}{"Prompt",-30}{"Cases",-7}{"Failures",-10}{"Avg dist",-12}{"Avg sim",-10}");
        Console.WriteLine(new string('-', 74));

        var rank = 1;
        foreach (var score in scores)
        {
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-5}{1,-30}{2,-7}{3,-10}{4,-12:0.00}{5,-10:0.0000}",
                rank++,
                Truncate(score.PromptName, 28),
                score.CasesEvaluated,
                score.Failures,
                score.AverageDistance,
                score.AverageSimilarity));
        }

        if (scores.Count > 0)
        {
            var winner = scores[0];
            Console.WriteLine();
            Console.WriteLine(
                $"Best prompt: '{winner.PromptName}' " +
                $"(avg similarity {winner.AverageSimilarity:0.0000}, avg distance {winner.AverageDistance:0.00})");
        }

        Console.WriteLine();
    }

    private static async Task WriteCsv(
        IReadOnlyList<PromptScore> scores,
        string path,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

        var builder = new StringBuilder();
        builder.AppendLine(
            "prompt,case,attempt,distance,similarity,case_avg_distance,case_avg_similarity,error,reference,produced");

        foreach (var score in scores)
        {
            foreach (var result in score.Results)
            {
                if (result.Attempts.Count == 0)
                {
                    // A fatal case-level failure with no attempts to enumerate — emit a single placeholder row.
                    builder.Append(Csv(result.PromptName)).Append(',')
                           .Append(Csv(result.CaseName)).Append(',')
                           .Append('0').Append(',')
                           .Append("0").Append(',')
                           .Append("0").Append(',')
                           .Append(Format(result.AverageDistance)).Append(',')
                           .Append(Format(result.AverageSimilarity)).Append(',')
                           .Append(Csv(result.Error ?? string.Empty)).Append(',')
                           .Append(Csv(result.ReferenceText)).Append(',')
                           .Append(Csv(string.Empty))
                           .AppendLine();
                    continue;
                }

                foreach (var attempt in result.Attempts)
                {
                    builder.Append(Csv(result.PromptName)).Append(',')
                           .Append(Csv(result.CaseName)).Append(',')
                           .Append(attempt.AttemptIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
                           .Append(attempt.LevenshteinDistance.ToString(CultureInfo.InvariantCulture)).Append(',')
                           .Append(Format(attempt.Similarity)).Append(',')
                           .Append(Format(result.AverageDistance)).Append(',')
                           .Append(Format(result.AverageSimilarity)).Append(',')
                           .Append(Csv(attempt.Error ?? result.Error ?? string.Empty)).Append(',')
                           .Append(Csv(result.ReferenceText)).Append(',')
                           .Append(Csv(attempt.ProducedText))
                           .AppendLine();
                }
            }
        }

        await File.WriteAllTextAsync(fullPath, builder.ToString(), cancellationToken);
    }

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string Format(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
