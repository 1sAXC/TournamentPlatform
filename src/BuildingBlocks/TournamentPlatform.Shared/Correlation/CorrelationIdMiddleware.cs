using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TournamentPlatform.Shared.Correlation;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ICorrelationContext correlationContext,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString("N");

        correlationContext.CorrelationId = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await next(context);
        }
    }
}
