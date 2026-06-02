namespace NeuroNotes.GitHub.Application;

/// <summary>
/// Parses the many shapes a user might send for a GitHub repository into an owner/repo pair:
/// <c>https://github.com/owner/repo(.git)</c>, <c>github.com/owner/repo</c>,
/// <c>git@github.com:owner/repo.git</c>, or a bare <c>owner/repo</c> slug.
/// </summary>
public static class GitHubRepositoryReference
{
    private const string Host = "github.com";
    private const string SshPrefix = "git@github.com:";

    public static bool TryParse(string? input, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var value = input.Trim();

        if (value.StartsWith(SshPrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[SshPrefix.Length..];
        }
        else if (value.Contains(Host, StringComparison.OrdinalIgnoreCase))
        {
            var hostEnd = value.IndexOf(Host, StringComparison.OrdinalIgnoreCase) + Host.Length;
            value = value[hostEnd..].TrimStart('/', ':');
        }

        // Drop any query string or fragment, then a trailing ".git" and slashes.
        value = value.Split('?', '#')[0];
        if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        value = value.Trim('/');

        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        owner = parts[0];
        repo = parts[1];
        return owner.Length > 0 && repo.Length > 0;
    }
}