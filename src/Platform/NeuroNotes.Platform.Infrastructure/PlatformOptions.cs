namespace NeuroNotes.Platform.Infrastructure;

public sealed record PlatformOptions
{
    public const string SectionName = "Platform";

    /// <summary>Static admin API key required on every <c>/admin/*</c> request (SEC-005).</summary>
    [Required]
    public required string AdminApiKey { get; init; }

    /// <summary>Directory operator-uploaded behavior extension assemblies are stored in and loaded from.</summary>
    public string PluginsDirectory { get; init; } = "plugins";

    /// <summary>Base URL bots' webhooks are registered under (each bot's is <c>{WebhookBaseUrl}/{botId}</c>); required outside Development.</summary>
    public string? WebhookBaseUrl { get; init; }
}