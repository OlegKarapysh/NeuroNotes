using System.ComponentModel.DataAnnotations;

namespace NeuroNotes.PromptEvaluation.Configuration;

public sealed record PromptEvaluationOptions
{
    public const string SectionName = "PromptEvaluation";

    /// <summary>
    /// Folder containing paired audio + reference files (e.g. <c>case1.ogg</c> + <c>case1.txt</c>).
    /// </summary>
    [Required]
    public string TestCasesDirectory { get; set; } = "test-data/cases";

    /// <summary>
    /// Folder containing one <c>*.txt</c> file per candidate system prompt.
    /// </summary>
    [Required]
    public string PromptsDirectory { get; set; } = "test-data/prompts";

    /// <summary>
    /// Optional path for a per-case CSV breakdown. Leave empty to skip.
    /// </summary>
    public string? CsvReportPath { get; set; } = "test-data/report.csv";

    /// <summary>
    /// Lower-cases text before measuring distance.
    /// </summary>
    public bool LowerCase { get; set; } = false;

    /// <summary>
    /// Strips punctuation and symbols before measuring distance.
    /// </summary>
    public bool StripPunctuation { get; set; } = false;

    /// <summary>
    /// How many enhancement attempts to run per (prompt, case) pair.
    /// The case score is the average of successful attempts, which reduces variance
    /// caused by LLM sampling.
    /// </summary>
    [Range(1, 100)]
    public int AttemptsPerCase { get; set; } = 5;
}
