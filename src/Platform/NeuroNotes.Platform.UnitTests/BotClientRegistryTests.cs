using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.Platform.Infrastructure.Bots;

namespace NeuroNotes.Platform.UnitTests;

public class BotClientRegistryTests
{
    [Fact]
    public void Get_Throws_WhenNoClientRegistered()
    {
        var registry = CreateRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.Get(botId: 1));
    }

    [Fact]
    public void Set_ThenGet_ReturnsAClient()
    {
        var registry = CreateRegistry();

        registry.Set(botId: 1, "123456:fake-token");

        Assert.NotNull(registry.Get(botId: 1));
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenNotRegistered()
    {
        var registry = CreateRegistry();

        Assert.False(registry.TryGet(botId: 999, out var client));
        Assert.Null(client);
    }

    [Fact]
    public void Remove_ForgetsTheClient()
    {
        var registry = CreateRegistry();
        registry.Set(botId: 1, "123456:fake-token");

        registry.Remove(botId: 1);

        Assert.False(registry.TryGet(botId: 1, out _));
    }

    [Fact]
    public void Set_KeepsClients_SeparatePerBot()
    {
        var registry = CreateRegistry();

        registry.Set(botId: 1, "111111:fake-token-a");
        registry.Set(botId: 2, "222222:fake-token-b");

        Assert.NotSame(registry.Get(botId: 1), registry.Get(botId: 2));
    }

    private static BotClientRegistry CreateRegistry()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var httpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        return new BotClientRegistry(httpClientFactory);
    }
}