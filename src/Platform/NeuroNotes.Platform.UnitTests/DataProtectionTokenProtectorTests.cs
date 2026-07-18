using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.Platform.Infrastructure.Security;

namespace NeuroNotes.Platform.UnitTests;

public class DataProtectionTokenProtectorTests
{
    [Fact]
    public void Protect_ThenUnprotect_RoundTripsTheOriginalToken()
    {
        const string token = "123456:AAExampleTelegramBotToken";
        var protector = CreateProtector();

        var encrypted = protector.Protect(token);

        Assert.NotEqual(token, Encoding.UTF8.GetString(encrypted));
        Assert.Equal(token, protector.Unprotect(encrypted));
    }

    [Fact]
    public void Protect_ProducesDifferentCiphertext_ForDifferentTokens()
    {
        var protector = CreateProtector();

        var first = protector.Protect("111111:TokenOne");
        var second = protector.Protect("222222:TokenTwo");

        Assert.NotEqual(first, second);
    }

    private static DataProtectionTokenProtector CreateProtector()
    {
        var services = new ServiceCollection();
        // Ephemeral: keys live only in memory for this test run — no real filesystem I/O (test purity).
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        return new DataProtectionTokenProtector(provider);
    }
}