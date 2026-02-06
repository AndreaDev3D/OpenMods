using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;
using System.Security.Claims;

namespace OpenMods.Server.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Supabase.Client _supabaseClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private bool _initialized = false;

    public SupabaseAuthStateProvider(Supabase.Client supabaseClient, IHttpContextAccessor httpContextAccessor)
    {
        _supabaseClient = supabaseClient;
        _httpContextAccessor = httpContextAccessor;
        
        _supabaseClient.Auth.AddStateChangedListener((sender, state) =>
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        });
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // On initial server load, try to recover session from cookies
        if (!_initialized && _supabaseClient.Auth.CurrentSession == null)
        {
            var cookieSession = _httpContextAccessor.HttpContext?.Request.Cookies["supabase_session"];
            if (!string.IsNullOrEmpty(cookieSession))
            {
                try 
                {
                    var sessionData = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionData>(cookieSession);
                    if (sessionData != null && !string.IsNullOrEmpty(sessionData.AccessToken) && !string.IsNullOrEmpty(sessionData.RefreshToken))
                    {
                        await _supabaseClient.Auth.SetSession(sessionData.AccessToken, sessionData.RefreshToken);
                        _initialized = true;
                    }
                }
                catch { /* Ignore invalid session */ }
            }
        }

        var session = _supabaseClient.Auth.CurrentSession;
        var user = session?.User;

        if (user == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("avatar_url", user.UserMetadata?.ContainsKey("avatar_url") == true ? user.UserMetadata["avatar_url"].ToString() ?? "" : ""),
            new Claim("github_handle", user.UserMetadata?.ContainsKey("user_name") == true ? user.UserMetadata["user_name"].ToString() ?? "" : ""),
            new Claim(ClaimTypes.Name, user.UserMetadata?.ContainsKey("full_name") == true ? user.UserMetadata["full_name"].ToString() ?? "" : user.Email ?? "")
        };

        var identity = new ClaimsIdentity(claims, "Supabase");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    private class SessionData
    {
        [Newtonsoft.Json.JsonProperty("access_token")]
        public string? AccessToken { get; set; }
        
        [Newtonsoft.Json.JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
