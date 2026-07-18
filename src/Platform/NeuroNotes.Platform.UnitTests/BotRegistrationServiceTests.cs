using System.Text;
using FluentResults;
using NeuroNotes.Platform.Application;
using NeuroNotes.Platform.Public;
using NeuroNotes.Platform.Public.Behaviors;
using NeuroNotes.Platform.Public.Bots;

namespace NeuroNotes.Platform.UnitTests;

public class BotRegistrationServiceTests
{
    [Fact]
    public async Task Register_Succeeds_AndStartsTheBot()
    {
        var registry = new FakeBotRegistry();
        var lifecycle = new FakeBotLifecycle();
        var service = CreateService(registry, lifecycle: lifecycle);

        var result = await service.Register("My Bot", "note-capture", "111:token", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("note-capture", result.Value.BehaviorKey);
        Assert.Equal(BotStatus.Active, result.Value.Status);
        Assert.Single(lifecycle.Started);
        Assert.Equal((result.Value.Id, "111:token"), lifecycle.Started[0]);
    }

    [Fact]
    public async Task Register_Fails_ForUnknownBehavior()
    {
        var service = CreateService();

        var result = await service.Register("My Bot", "unknown-behavior", "111:token", TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
        Assert.Contains("Unknown behavior", result.Errors.First().Message);
    }

    [Fact]
    public async Task Register_Fails_WhenTokenValidationFails()
    {
        var lifecycle = new FakeBotLifecycle();
        var service = CreateService(validator: new FakeTokenValidator(succeeds: false), lifecycle: lifecycle);

        var result = await service.Register("My Bot", "note-capture", "bad-token", TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
        Assert.Empty(lifecycle.Started);
    }

    [Fact]
    public async Task Register_Fails_WhenTelegramBotAlreadyRegistered()
    {
        var registry = new FakeBotRegistry();
        var service = CreateService(registry);
        await service.Register("First", "note-capture", "111:token", TestContext.Current.CancellationToken);

        var result = await service.Register("Second", "note-capture", "111:token-again", TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Disable_SetsStatus_AndStopsTheBot()
    {
        var registry = new FakeBotRegistry();
        var lifecycle = new FakeBotLifecycle();
        var service = CreateService(registry, lifecycle: lifecycle);
        var bot = (await service.Register("My Bot", "note-capture", "111:token", TestContext.Current.CancellationToken)).Value;

        var result = await service.Disable(bot.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(BotStatus.Disabled, (await service.Get(bot.Id, TestContext.Current.CancellationToken))!.Status);
        Assert.Contains(bot.Id, lifecycle.Stopped);
    }

    [Fact]
    public async Task Enable_StartsTheBot_WithTheDecryptedToken()
    {
        var registry = new FakeBotRegistry();
        var lifecycle = new FakeBotLifecycle();
        var service = CreateService(registry, lifecycle: lifecycle);
        var bot = (await service.Register("My Bot", "note-capture", "111:token", TestContext.Current.CancellationToken)).Value;
        await service.Disable(bot.Id, TestContext.Current.CancellationToken);
        lifecycle.Started.Clear();

        var result = await service.Enable(bot.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(BotStatus.Active, (await service.Get(bot.Id, TestContext.Current.CancellationToken))!.Status);
        Assert.Equal((bot.Id, "111:token"), Assert.Single(lifecycle.Started));
    }

    [Fact]
    public async Task RotateToken_StartsWithTheNewToken_WhenSameTelegramBot()
    {
        var registry = new FakeBotRegistry();
        var lifecycle = new FakeBotLifecycle();
        var validator = new FakeTokenValidator(telegramBotId: 111);
        var service = CreateService(registry, validator, lifecycle);
        var bot = (await service.Register("My Bot", "note-capture", "111:old-token", TestContext.Current.CancellationToken)).Value;
        lifecycle.Started.Clear();

        var result = await service.RotateToken(bot.Id, "111:new-token", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal((bot.Id, "111:new-token"), Assert.Single(lifecycle.Started));
    }

    [Fact]
    public async Task RotateToken_Fails_WhenTokenBelongsToADifferentTelegramBot()
    {
        var registry = new FakeBotRegistry();
        var lifecycle = new FakeBotLifecycle();
        var service = CreateService(registry, lifecycle: lifecycle);
        var bot = (await service.Register("My Bot", "note-capture", "111:token", TestContext.Current.CancellationToken)).Value;
        var mismatchedValidator = new FakeTokenValidator(telegramBotId: 999);
        var serviceWithMismatch = CreateService(registry, mismatchedValidator, lifecycle);
        lifecycle.Started.Clear();

        var result = await serviceWithMismatch.RotateToken(bot.Id, "999:other-bot-token", TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
        Assert.Empty(lifecycle.Started);
    }

    [Fact]
    public async Task Remove_RemovesFromRegistryAndLifecycle()
    {
        var registry = new FakeBotRegistry();
        var lifecycle = new FakeBotLifecycle();
        var service = CreateService(registry, lifecycle: lifecycle);
        var bot = (await service.Register("My Bot", "note-capture", "111:token", TestContext.Current.CancellationToken)).Value;

        var result = await service.Remove(bot.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Null(await service.Get(bot.Id, TestContext.Current.CancellationToken));
        Assert.Contains(bot.Id, lifecycle.Removed);
    }

    private static BotRegistrationService CreateService(
        FakeBotRegistry? registry = null,
        FakeTokenValidator? validator = null,
        FakeBotLifecycle? lifecycle = null) =>
        new(registry ?? new FakeBotRegistry(),
            new FakeBehaviorCatalog(),
            new FakeTokenProtector(),
            validator ?? new FakeTokenValidator(),
            lifecycle ?? new FakeBotLifecycle());

    private sealed class FakeBotRegistry : IBotRegistry
    {
        private long _nextId = 1;
        private readonly Dictionary<long, (BotRegistration Registration, byte[] Token)> _bots = new();

        public Task<Result<BotRegistration>> AddAsync(long telegramBotId, string? username, string label, string behaviorKey, byte[] encryptedToken, CancellationToken cancellationToken = default)
        {
            if (_bots.Values.Any(b => b.Registration.TelegramBotId == telegramBotId))
            {
                return Task.FromResult(Result.Fail<BotRegistration>($"Telegram bot {telegramBotId} is already registered."));
            }

            var registration = new BotRegistration(_nextId++, telegramBotId, username, label, behaviorKey, BotStatus.Active, DateTime.UtcNow, DateTime.UtcNow);
            _bots[registration.Id] = (registration, encryptedToken);
            return Task.FromResult(Result.Ok(registration));
        }

        public Task<BotRegistration?> GetAsync(long botId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_bots.TryGetValue(botId, out var entry) ? entry.Registration : null);

        public Task<byte[]?> GetEncryptedTokenAsync(long botId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_bots.TryGetValue(botId, out var entry) ? entry.Token : null);

        public Task<IReadOnlyList<BotRegistration>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotRegistration>>(_bots.Values.Select(b => b.Registration).ToArray());

        public Task<Result> UpdateStatusAsync(long botId, BotStatus status, CancellationToken cancellationToken = default)
        {
            if (!_bots.TryGetValue(botId, out var entry))
            {
                return Task.FromResult(Result.Fail($"Bot {botId} was not found."));
            }

            _bots[botId] = (entry.Registration with { Status = status }, entry.Token);
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> UpdateTokenAsync(long botId, long telegramBotId, byte[] encryptedToken, CancellationToken cancellationToken = default)
        {
            if (!_bots.TryGetValue(botId, out var entry))
            {
                return Task.FromResult(Result.Fail($"Bot {botId} was not found."));
            }

            if (entry.Registration.TelegramBotId != telegramBotId)
            {
                return Task.FromResult(Result.Fail("The new token belongs to a different Telegram bot than the one currently registered."));
            }

            _bots[botId] = (entry.Registration, encryptedToken);
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> RemoveAsync(long botId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_bots.Remove(botId) ? Result.Ok() : Result.Fail($"Bot {botId} was not found."));
    }

    private sealed class FakeTokenValidator(bool succeeds = true, long telegramBotId = 111, string username = "bot") : IBotTokenValidator
    {
        public Task<Result<(long TelegramBotId, string? Username)>> Validate(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(succeeds
                ? Result.Ok<(long, string?)>((telegramBotId, username))
                : Result.Fail<(long, string?)>("Telegram rejected the bot token."));
    }

    private sealed class FakeTokenProtector : ITokenProtector
    {
        public byte[] Protect(string plaintextToken) => Encoding.UTF8.GetBytes(plaintextToken);
        public string Unprotect(byte[] protectedToken) => Encoding.UTF8.GetString(protectedToken);
    }

    private sealed class FakeBehaviorCatalog : IBehaviorCatalog
    {
        public bool TryGet(string key, out IBotBehavior? behavior)
        {
            behavior = null;
            return key == "note-capture";
        }

        public IReadOnlyList<BehaviorDescriptor> List() => [];

        public Result Register(IBotBehavior behavior, string source) => Result.Ok();
    }

    private sealed class FakeBotLifecycle : IBotLifecycle
    {
        public List<(long BotId, string Token)> Started { get; } = [];
        public List<long> Stopped { get; } = [];
        public List<long> Removed { get; } = [];

        public Task StartAsync(long botId, string token, CancellationToken cancellationToken = default)
        {
            Started.Add((botId, token));
            return Task.CompletedTask;
        }

        public Task StopAsync(long botId, CancellationToken cancellationToken = default)
        {
            Stopped.Add(botId);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(long botId, CancellationToken cancellationToken = default)
        {
            Removed.Add(botId);
            return Task.CompletedTask;
        }
    }
}