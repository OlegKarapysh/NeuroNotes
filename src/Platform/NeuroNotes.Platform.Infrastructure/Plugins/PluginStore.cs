namespace NeuroNotes.Platform.Infrastructure.Plugins;

/// <summary>
/// Persists uploaded behavior-extension assemblies under <see cref="PlatformOptions.PluginsDirectory"/>
/// so they can be reloaded after a restart (in addition to being loaded immediately on upload).
/// </summary>
public sealed class PluginStore(IOptions<PlatformOptions> platformOptions)
{
    private string Directory => platformOptions.Value.PluginsDirectory;

    /// <summary>Whether an extension assembly with this (path-stripped) name is already persisted.</summary>
    public bool Exists(string fileName) => File.Exists(Path.Combine(Directory, Path.GetFileName(fileName)));

    /// <summary>Best-effort removal of a persisted assembly — used to clean up a rejected upload.</summary>
    public void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        System.IO.Directory.CreateDirectory(Directory);

        // Strip any path segments from the client-supplied name so an upload can never escape the plugins directory.
        var safeFileName = Path.GetFileName(fileName);
        var path = Path.Combine(Directory, safeFileName);

        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);

        return path;
    }

    /// <summary>Every previously-uploaded extension assembly, for reloading on startup.</summary>
    public IReadOnlyList<string> ListStoredAssemblyPaths() =>
        System.IO.Directory.Exists(Directory)
            ? System.IO.Directory.GetFiles(Directory, "*.dll")
            : [];
}