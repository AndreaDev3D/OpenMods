using Supabase;
using Supabase.Interfaces;

namespace OpenMods.Server.Services;

public class AuthService
{
    private readonly Client _supabaseClient;
    private readonly IConfiguration _configuration;

    public AuthService(Client supabaseClient, IConfiguration configuration)
    {
        _supabaseClient = supabaseClient;
        _configuration = configuration;
    }

    public string GetGitHubLoginUrl()
    {
        var redirectUrl = _configuration["SUPABASE_CALLBACK_URL"] ?? "https://ysatdqbgihomvhvjuvda.supabase.co/auth/v1/callback";
        
        // Supabase Auth URL construction for GitHub
        var options = new Supabase.Gotrue.SignInOptions
        {
            RedirectTo = redirectUrl
        };
        
        // Return the provider-specific sign-in URL
        // In the csharp client, we usually use SignIn(Provider provider)
        // For Blazor Server, we might need a different approach or use the Supabase client to handle it.
        return ""; // We will handle this in the Razor component for now using the client directly
    }

    public async Task<Supabase.Gotrue.Session?> GetCurrentSession()
    {
        return _supabaseClient.Auth.CurrentSession;
    }

    public async Task SignOut()
    {
        await _supabaseClient.Auth.SignOut();
    }
}
