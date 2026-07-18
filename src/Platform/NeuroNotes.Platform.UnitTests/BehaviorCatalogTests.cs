using NeuroNotes.Platform.Application;
using NeuroNotes.Platform.Public.Behaviors;

namespace NeuroNotes.Platform.UnitTests;

public class BehaviorCatalogTests
{
    private sealed class FakeBehavior(string key, string contractVersion = "1.0") : IBotBehavior
    {
        public string Key { get; } = key;
        public string DisplayName => $"Fake:{Key}";
        public string ContractVersion { get; } = contractVersion;
        public Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public void Register_Succeeds_ForNewKey()
    {
        var catalog = new BehaviorCatalog();

        var result = catalog.Register(new FakeBehavior("echo"), "built-in");

        Assert.True(result.IsSuccess);
        Assert.True(catalog.TryGet("echo", out var behavior));
        Assert.Equal("echo", behavior!.Key);
    }

    [Fact]
    public void Register_Fails_WhenKeyAlreadyRegistered()
    {
        var catalog = new BehaviorCatalog();
        catalog.Register(new FakeBehavior("echo"), "built-in");

        var result = catalog.Register(new FakeBehavior("echo"), "extension:Echo.dll");

        Assert.True(result.IsFailed);
        Assert.Contains("already registered", result.Errors.First().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_Fails_WhenContractMajorVersionDiffers()
    {
        var catalog = new BehaviorCatalog();

        var result = catalog.Register(new FakeBehavior("echo", "2.0"), "extension:Echo.dll");

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Register_Succeeds_WhenOnlyMinorVersionDiffers()
    {
        var catalog = new BehaviorCatalog();

        var result = catalog.Register(new FakeBehavior("echo", "1.5"), "extension:Echo.dll");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownKey()
    {
        var catalog = new BehaviorCatalog();

        Assert.False(catalog.TryGet("missing", out var behavior));
        Assert.Null(behavior);
    }

    [Fact]
    public void List_ReturnsAllRegisteredBehaviors_WithTheirSource()
    {
        var catalog = new BehaviorCatalog();
        catalog.Register(new FakeBehavior("note-capture"), "built-in");
        catalog.Register(new FakeBehavior("echo"), "extension:Echo.dll");

        var descriptors = catalog.List();

        Assert.Equal(2, descriptors.Count);
        Assert.Contains(descriptors, d => d.Key == "note-capture" && d.Source == "built-in");
        Assert.Contains(descriptors, d => d.Key == "echo" && d.Source == "extension:Echo.dll");
    }
}