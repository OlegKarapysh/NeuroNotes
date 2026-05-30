using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using NeuroNotes.PromptEvaluation.Configuration;

namespace NeuroNotes.PromptEvaluation.Metrics;

/// <summary>
/// Normalizes text before scoring so cosmetic differences (case, whitespace, punctuation)
/// don't inflate the Levenshtein distance.
/// </summary>
public sealed class DefaultTextNormalizer(IOptions<PromptEvaluationOptions> options) : ITextNormalizer
{
    private readonly PromptEvaluationOptions _options = options.Value;

    public string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var previousWasWhitespace = true; // collapse leading whitespace

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }
                continue;
            }

            if (_options.StripPunctuation && IsPunctuation(ch))
            {
                continue;
            }

            builder.Append(_options.LowerCase
                ? char.ToLower(ch, CultureInfo.InvariantCulture)
                : ch);
            previousWasWhitespace = false;
        }

        // Trim trailing whitespace produced by the collapser above.
        while (builder.Length > 0 && builder[^1] == ' ')
        {
            builder.Length--;
        }

        return builder.ToString();
    }

    private static bool IsPunctuation(char ch) =>
        char.IsPunctuation(ch) || char.IsSymbol(ch);
}
