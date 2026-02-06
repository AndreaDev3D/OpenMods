using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Supabase.Gotrue;
using System.Security.Claims;

namespace OpenMods.Server.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly Supabase.Client _supabaseClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private bool _initialized = false;

    public SupabaseAuthStateProvider(
        Supabase.Client supabaseClient,
        IHttpContextAccessor httpContextAccessor,
        PersistentComponentState state)
    {
        _supabaseClient = supabaseClient;
        _httpContextAccessor = httpContextAccessor;
        _state = state;

        _subscription = _state.RegisterOnPersisting(PersistSession);

        _supabaseClient.Auth.AddStateChangedListener((sender, state) =>
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        });
    }

    private Task PersistSession()
    {
        var session = _supabaseClient.Auth.CurrentSession;
        if (session != null)
        {
            _state.PersistAsJson("supabase_session_data", new SessionData
            {
                AccessToken = session.AccessToken,
                RefreshToken = session.RefreshToken
            });
        }
        return Task.CompletedTask;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // 1. Try to recover from PersistentComponentState (Interactive Circuit)
        if (!_initialized && _supabaseClient.Auth.CurrentSession == null)
        {
            if (_state.TryTakeFromJson<SessionData>("supabase_session_data", out var restored))
            {
                if (restored != null && !string.IsNullOrEmpty(restored.AccessToken))
                {
                    try
                    {
                        Console.WriteLine("[DEBUG] AuthState: Found state in PersistentComponentState. Restoring...");
                        await _supabaseClient.Auth.SetSession(restored.AccessToken, restored.RefreshToken ?? "");
                        _initialized = true;
                        Console.WriteLine("[DEBUG] AuthState: Successfully restored session from PersistentComponentState");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] AuthState: Error restoring from state: {ex.Message}");
                    }
                }
            }
        }

        // 2. Fallback to Cookie (Prerendering or first load)
        if (!_initialized && _supabaseClient.Auth.CurrentSession == null)
        {
            var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies["supabase_session"];
            if (!string.IsNullOrEmpty(cookieValue))
            {
                try
                {
                    // Decode from Base64
                    var base64Bytes = Convert.FromBase64String(cookieValue);
                    var sessionJson = System.Text.Encoding.UTF8.GetString(base64Bytes);

                    var sessionData = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionData>(sessionJson);
                    if (sessionData != null && !string.IsNullOrEmpty(sessionData.AccessToken) && !string.IsNullOrEmpty(sessionData.RefreshToken))
                    {
                        Console.WriteLine("[DEBUG] AuthState: Found Base64 session cookie. Restoring...");

                        // Prevent re-entry BEFORE calling SetSession because it triggers an event
                        _initialized = true;
                        await _supabaseClient.Auth.SetSession(sessionData.AccessToken, sessionData.RefreshToken);

                        Console.WriteLine("[DEBUG] AuthState: Successfully restored session from Base64 Cookie");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] AuthState: Error restoring from cookie: {ex.Message}");
                    _initialized = false; // Reset if failed
                }
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

    public void Dispose()
    {
        _subscription.Dispose();
    }

    private class SessionData
    {
        [Newtonsoft.Json.JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [Newtonsoft.Json.JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
