using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RcmEngine.Data;
using RcmEngine.Services;

namespace PlatformApi.Auth;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    RcmDbContext db,
    IConfiguration config) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrEmpty(apiKey))
            return AuthenticateResult.Fail("Invalid API key");

        var hash = DataSeeder.HashApiKey(apiKey);
        var key = await db.ApiKeys
            .Include(k => k.Organization)
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);

        if (key == null)
        {
            var demoKey = config["ApiKeys:DemoKey"];
            if (apiKey == demoKey)
            {
                var org = await db.Organizations.FirstOrDefaultAsync();
                if (org != null)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, "demo-api-key"),
                        new Claim("org_id", org.Id.ToString()),
                        new Claim(ClaimTypes.Role, "APIPartner")
                    };
                    return AuthenticateResult.Success(new AuthenticationTicket(
                        new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
                }
            }
            return AuthenticateResult.Fail("Invalid API key");
        }

        key.LastUsedAt = DateTime.UtcNow;
        key.UsageCount++;
        await db.SaveChangesAsync();

        var keyClaims = new[]
        {
            new Claim(ClaimTypes.Name, key.Name),
            new Claim("org_id", key.OrganizationId.ToString()),
            new Claim("api_key_id", key.Id.ToString()),
            new Claim(ClaimTypes.Role, "APIPartner")
        };

        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(keyClaims, Scheme.Name)), Scheme.Name));
    }
}

public static class TenantExtensions
{
    public static Guid GetOrganizationId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("org_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
