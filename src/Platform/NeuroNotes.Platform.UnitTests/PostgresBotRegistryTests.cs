using NeuroNotes.Platform.Persistence.Repositories;
using NeuroNotes.Platform.Public.Bots;

namespace NeuroNotes.Platform.UnitTests;

public class PostgresBotRegistryTests
{
    private static readonly byte[] Token = "encrypted-token"u8.ToArray();

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotRegistered()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);

        Assert.Null(await registry.GetAsync(botId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddAsync_ThenGetAsync_ReturnsRegistration()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);

        var addResult = await registry.AddAsync(
            telegramBotId: 111, "note_bot", "Notes", "note-capture", Token, TestContext.Current.CancellationToken);

        Assert.True(addResult.IsSuccess);
        var registration = addResult.Value;
        Assert.Equal(111, registration.TelegramBotId);
        Assert.Equal("note_bot", registration.Username);
        Assert.Equal("Notes", registration.Label);
        Assert.Equal("note-capture", registration.BehaviorKey);
        Assert.Equal(BotStatus.Active, registration.Status);

        var fetched = await registry.GetAsync(registration.Id, TestContext.Current.CancellationToken);
        Assert.Equal(registration, fetched);
    }

    [Fact]
    public async Task AddAsync_Fails_WhenTelegramBotIdAlreadyRegistered()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        await registry.AddAsync(111, "note_bot", "Notes", "note-capture", Token, TestContext.Current.CancellationToken);

        var result = await registry.AddAsync(111, "note_bot", "Notes Again", "note-capture", Token, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
        Assert.Contains("already registered", result.Errors.First().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEncryptedTokenAsync_ReturnsStoredCiphertext()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        var registration = (await registry.AddAsync(111, "note_bot", "Notes", "note-capture", Token, TestContext.Current.CancellationToken)).Value;

        var storedToken = await registry.GetEncryptedTokenAsync(registration.Id, TestContext.Current.CancellationToken);

        Assert.Equal(Token, storedToken);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllRegisteredBots()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        await registry.AddAsync(111, "bot_a", "A", "note-capture", Token, TestContext.Current.CancellationToken);
        await registry.AddAsync(222, "bot_b", "B", "note-capture", Token, TestContext.Current.CancellationToken);

        var bots = await registry.ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, bots.Count);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatus()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        var registration = (await registry.AddAsync(111, "bot_a", "A", "note-capture", Token, TestContext.Current.CancellationToken)).Value;

        var updateResult = await registry.UpdateStatusAsync(registration.Id, BotStatus.Disabled, TestContext.Current.CancellationToken);

        Assert.True(updateResult.IsSuccess);
        var fetched = await registry.GetAsync(registration.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BotStatus.Disabled, fetched!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_Fails_WhenBotDoesNotExist()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);

        var result = await registry.UpdateStatusAsync(botId: 999, BotStatus.Disabled, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task UpdateTokenAsync_ReplacesToken_WhenTelegramBotIdMatches()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        var registration = (await registry.AddAsync(111, "bot_a", "A", "note-capture", Token, TestContext.Current.CancellationToken)).Value;
        var newToken = "new-encrypted-token"u8.ToArray();

        var result = await registry.UpdateTokenAsync(registration.Id, 111, newToken, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(newToken, await registry.GetEncryptedTokenAsync(registration.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateTokenAsync_Fails_WhenTelegramBotIdDiffers()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        var registration = (await registry.AddAsync(111, "bot_a", "A", "note-capture", Token, TestContext.Current.CancellationToken)).Value;

        var result = await registry.UpdateTokenAsync(registration.Id, 999, "irrelevant"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
        Assert.Contains("different Telegram bot", result.Errors.First().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveAsync_DeletesRegistration()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);
        var registration = (await registry.AddAsync(111, "bot_a", "A", "note-capture", Token, TestContext.Current.CancellationToken)).Value;

        var result = await registry.RemoveAsync(registration.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Null(await registry.GetAsync(registration.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_Fails_WhenBotDoesNotExist()
    {
        await using var dbContext = InMemoryDbContextFactory.Create();
        var registry = new PostgresBotRegistry(dbContext);

        var result = await registry.RemoveAsync(botId: 999, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailed);
    }
}