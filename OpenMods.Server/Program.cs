using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;
using OpenMods.Server.Services;
using OpenMods.Server.Components;
using OpenMods.Server.Data;

// Load environment variables from .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}
else 
{
    // Try parent directory (useful when running from bin folder)
    envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Support for Koyeb/Docker dynamic port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Diagnostic logging for environment
Console.WriteLine($"[DEBUG] SUPABASE_URL: {Environment.GetEnvironmentVariable("SUPABASE_URL")}");
Console.WriteLine($"[DEBUG] APP_URL: {Environment.GetEnvironmentVariable("APP_URL")}");
Console.WriteLine($"[DEBUG] PORT: {port}");

// Add services to the container.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")));

// Configure Forwarded Headers for Koyeb/Docker
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient<GitHubService>();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
builder.Services.AddAuthentication("Supabase")
    .AddCookie("Supabase", options =>
    {
        options.LoginPath = "/";
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
// Configure Supabase Client
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
builder.Services.AddScoped(provider => new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
}));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

var app = builder.Build();

// Must be first
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Disable HTTPS redirection when running in Docker/Koyeb (which usually handle SSL at the edge)
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true" && 
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KOYEB_APP_ID")))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
