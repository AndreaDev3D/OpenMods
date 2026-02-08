using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenMods.Shared.Services;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OpenMods.Server.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.NoResult();
        }

        var developerId = await _apiKeyService.ValidateApiKey(providedKey);
        if (developerId == null)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // Update last used timestamp (fire and forget or await)
        _ = _apiKeyService.UpdateLastUsed(providedKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, developerId.ToString()!),
            new Claim("DeveloperId", developerId.ToString()!)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
