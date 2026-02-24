using System.Security.Claims;
using System.Text.Encodings.Web;
using ClawdFiles.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClawdFiles.Web.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository keyRepo,
    IApiKeyHasher hasher,
    IConfiguration configuration)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var rawKey = headerValue["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(rawKey))
            return AuthenticateResult.Fail("Empty API key");

        var adminKey = configuration.GetValue<string>("AdminApiKey");
        var isAdmin = !string.IsNullOrEmpty(adminKey) && rawKey == adminKey;

        var hash = hasher.Hash(rawKey);
        var apiKey = await keyRepo.FindByHashAsync(hash);

        if (apiKey is null && !isAdmin)
            return AuthenticateResult.Fail("Invalid API key");

        var claims = new List<Claim> { new("api_key_prefix", apiKey?.Prefix ?? "admin") };

        if (apiKey is not null)
        {
            claims.Add(new Claim("api_key_id", apiKey.Id.ToString()));
            apiKey.LastUsedAt = DateTimeOffset.UtcNow;
            await keyRepo.UpdateAsync(apiKey);
        }

        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
