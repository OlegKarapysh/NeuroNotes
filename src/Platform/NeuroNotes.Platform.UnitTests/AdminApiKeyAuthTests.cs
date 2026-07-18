using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using NeuroNotes.Platform.Infrastructure;
using NeuroNotes.Platform.Infrastructure.Security;

namespace NeuroNotes.Platform.UnitTests;

public class AdminApiKeyAuthTests
{
    private const string ValidKey = "s3cr3t-admin-key";

    [Fact]
    public async Task InvokeAsync_Allows_WhenBearerKeyMatches()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {ValidKey}";
        var context = EndpointFilterInvocationContext.Create(httpContext);

        var result = await CreateFilter().InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok"));

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task InvokeAsync_Allows_WhenHeaderKeyMatches()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Admin-Api-Key"] = ValidKey;
        var context = EndpointFilterInvocationContext.Create(httpContext);

        var result = await CreateFilter().InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok"));

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task InvokeAsync_RejectsUnauthorized_WhenKeyIsMissing()
    {
        var httpContext = new DefaultHttpContext();
        var context = EndpointFilterInvocationContext.Create(httpContext);

        var result = await CreateFilter().InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok"));

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task InvokeAsync_RejectsUnauthorized_WhenKeyIsWrong()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Admin-Api-Key"] = "wrong-key";
        var context = EndpointFilterInvocationContext.Create(httpContext);

        var result = await CreateFilter().InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok"));

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    private static AdminApiKeyAuth CreateFilter() =>
        new(Options.Create(new PlatformOptions { AdminApiKey = ValidKey }));
}