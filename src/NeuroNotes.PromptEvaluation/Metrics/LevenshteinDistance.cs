namespace NeuroNotes.PromptEvaluation.Metrics;

/// <summary>
/// Classic Levenshtein edit distance with two-row dynamic programming.
/// Time: O(n*m). Space: O(min(n,m)).
/// </summary>
public static class LevenshteinDistance
{
    public static int Compute(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        if (source.Length == 0)
        {
            return target.Length;
        }

        if (target.Length == 0)
        {
            return source.Length;
        }

        // Always iterate over the shorter span on the inner loop to keep memory minimal.
        if (source.Length < target.Length)
        {
            var swap = source;
            source = target;
            target = swap;
        }

        var previous = new int[target.Length + 1];
        var current = new int[target.Length + 1];

        for (var j = 0; j <= target.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            var sourceChar = source[i - 1];

            for (var j = 1; j <= target.Length; j++)
            {
                var cost = sourceChar == target[j - 1] ? 0 : 1;
                var deletion = previous[j] + 1;
                var insertion = current[j - 1] + 1;
                var substitution = previous[j - 1] + cost;
                current[j] = Math.Min(Math.Min(deletion, insertion), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    /// <summary>
    /// Normalized similarity in [0, 1]. 1.0 means identical strings.
    /// </summary>
    public static decimal Similarity(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        var maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0)
        {
            return 1.0m;
        }

        var distance = Compute(source, target);
        return 1.0m - (decimal)distance / maxLength;
    }
}
