using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;

namespace PlatformApi.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Items["CorrelationId"] = correlationId;
        await next(context);
    }
}

public class ApiUsageLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RcmDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments("/v1"))
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = context.User.FindFirst("org_id")?.Value;
            var apiKeyIdClaim = context.User.FindFirst("api_key_id")?.Value;

            if (Guid.TryParse(orgIdClaim, out var orgId))
            {
                db.ApiUsageLogs.Add(new ApiUsageLog
                {
                    OrganizationId = orgId,
                    ApiKeyId = Guid.TryParse(apiKeyIdClaim, out var keyId) ? keyId : null,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    StatusCode = context.Response.StatusCode,
                    DurationMs = sw.ElapsedMilliseconds
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
