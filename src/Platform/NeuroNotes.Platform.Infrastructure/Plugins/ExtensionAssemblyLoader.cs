namespace NeuroNotes.Platform.Infrastructure.Plugins;

/// <summary>
/// Loads an operator-supplied behavior-extension assembly into its own collectible
/// <see cref="AssemblyLoadContext"/> and discovers its <see cref="IBotBehavior"/> implementations (FR-005).
/// A bad assembly — one that fails to load, or contains no usable behavior — is reported without touching
/// anything already running (FR-006); the platform and every existing bot are unaffected.
/// </summary>
public sealed class ExtensionAssemblyLoader
{
    public Result<IReadOnlyList<IBotBehavior>> Load(string assemblyPath)
    {
        try
        {
            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            var behaviors = assembly.GetExportedTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IBotBehavior).IsAssignableFrom(type))
                .Select(CreateInstance)
                .OfType<IBotBehavior>()
                .ToArray();

            if (behaviors.Length == 0)
            {
                return new Error($"Assembly \"{Path.GetFileName(assemblyPath)}\" does not contain any usable IBotBehavior implementation.");
            }

            return Result.Ok<IReadOnlyList<IBotBehavior>>(behaviors);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new Error($"Failed to load behavior extension \"{Path.GetFileName(assemblyPath)}\": {exception.Message}");
        }
    }

    private static IBotBehavior? CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type) as IBotBehavior;
        }
        catch
        {
            // A behavior that can't be constructed (e.g. no parameterless constructor) is simply skipped,
            // not a load failure — other behaviors in the same assembly can still be usable.
            return null;
        }
    }

    /// <summary>
    /// Shares the platform SDK and Telegram.Bot assemblies from the host's default load context (so a
    /// plugin's <see cref="IBotBehavior"/> instances unify with the host's interface type), resolving
    /// everything else — the plugin's own private dependencies — from alongside the plugin DLL.
    /// </summary>
    private sealed class PluginLoadContext : AssemblyLoadContext
    {
        private static readonly HashSet<string> SharedAssemblyNames = ["NeuroNotes.Platform.Public", "Telegram.Bot"];

        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is not null && SharedAssemblyNames.Contains(assemblyName.Name))
            {
                return null; // Fall back to the default ALC, unifying the type with the host's.
            }

            var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return resolvedPath is not null ? LoadFromAssemblyPath(resolvedPath) : null;
        }
    }
}