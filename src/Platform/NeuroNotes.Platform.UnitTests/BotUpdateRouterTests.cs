using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NeuroNotes.Platform.Application;
using NeuroNotes.Platform.Public.Behaviors;
using NeuroNotes.Platform.Public.Bots;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NeuroNotes.Platform.UnitTests;

public class BotUpdateRouterTests
{
    private static readonly Update SampleUpdate = new();

    [Fact]
    public async Task Route_DispatchesToTheBotsAssignedBehavior()
    {
        var registry = new FakeBotRegistry();
        registry.Seed(Registration(1, "recording"));
        var catalog = new BehaviorCatalog();
        var behavior = new RecordingBehavior();
        catalog.Register(behavior, "built-in");
        var router = CreateRouter(registry, catalog);

        await router.Route(botId: 1, SampleUpdate, TestContext.Current.CancellationToken);

        Assert.Equal(1, behavior.CallCount);
        Assert.Equal(1, behavior.LastContext!.BotId);
        Assert.Same(SampleUpdate, behavior.LastContext.Update);
    }

    [Fact]
    public async Task Route_DoesNothing_WhenBotIsUnknown()
    {
        var registry = new FakeBotRegistry();
        var catalog = new BehaviorCatalog();
        var behavior = new RecordingBehavior();
        catalog.Register(behavior, "built-in");
        var router = CreateRouter(registry, catalog);

        await router.Route(botId: 999, SampleUpdate, TestContext.Current.CancellationToken);

        Assert.Equal(0, behavior.CallCount);
    }

    [Fact]
    public async Task Route_DoesNothing_WhenBehaviorKeyIsUnknown()
    {
        var registry = new FakeBotRegistry();
        registry.Seed(Registration(1, "missing-behavior"));
        var catalog = new BehaviorCatalog();
        var router = CreateRouter(registry, catalog);

        var exception = await Record.ExceptionAsync(() => router.Route(botId: 1, SampleUpdate, TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Route_ContainsAFaultInOneBotsBehavior_WithoutThrowing()
    {
        var registry = new FakeBotRegistry();
        registry.Seed(Registration(1, "throwing"));
        var catalog = new BehaviorCatalog();
        catalog.Register(new ThrowingBehavior(), "built-in");
        var router = CreateRouter(registry, catalog);

        var exception = await Record.ExceptionAsync(() => router.Route(botId: 1, SampleUpdate, TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    private static BotUpdateRouter CreateRouter(IBotRegistry registry, IBehaviorCatalog catalog) =>
        new(registry,
            catalog,
            new TelegramBotClient("123456:fake-token-never-called"),
            new ServiceCollection().BuildServiceProvider(),
            new BotHealthTracker(registry, NullLogger<BotHealthTracker>.Instance),
            NullLogger<BotUpdateRouter>.Instance);

    private static BotRegistration Registration(long id, string behaviorKey) =>
        new(id, TelegramBotId: 1000 + id, Username: "bot", Label: "Bot", behaviorKey, BotStatus.Active, DateTime.UtcNow, DateTime.UtcNow);

    private sealed class FakeBotRegistry : IBotRegistry
    {
        private readonly Dictionary<long, BotRegistration> _bots = new();

        public void Seed(BotRegistration registration) => _bots[registration.Id] = registration;

        public Task<Result<BotRegistration>> AddAsync(long telegramBotId, string? username, string label, string behaviorKey, byte[] encryptedToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BotRegistration?> GetAsync(long botId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_bots.GetValueOrDefault(botId));

        public Task<byte[]?> GetEncryptedTokenAsync(long botId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<BotRegistration>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result> UpdateStatusAsync(long botId, BotStatus status, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result> UpdateTokenAsync(long botId, long telegramBotId, byte[] encryptedToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result> RemoveAsync(long botId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingBehavior : IBotBehavior
    {
        public int CallCount { get; private set; }
        public IBotUpdateContext? LastContext { get; private set; }

        public string Key => "recording";
        public string DisplayName => "Recording";
        public string ContractVersion => BehaviorContractVersion.Current;

        public Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            LastContext = context;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingBehavior : IBotBehavior
    {
        public string Key => "throwing";
        public string DisplayName => "Throwing";
        public string ContractVersion => BehaviorContractVersion.Current;

        public Task HandleUpdateAsync(IBotUpdateContext context, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }
}