using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;
using OpenMods.Shared.Services;
using OpenMods.Server.Services;
using OpenMods.Server.Components;
using OpenMods.Shared.Data;
using OpenMods.Server.Authentication;

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

// Support for Koyeb/Docker dynamic port (Skip in local dev to let VS manage HTTPS)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && !builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");

// Add services to the container.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")));

// Configure Forwarded Headers for Koyeb/Docker
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Trust all proxies in container environment
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = null; // Handle multiple proxy hops
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient<GitHubService>();
builder.Services.AddScoped<ModService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Supabase";
        options.DefaultChallengeScheme = "Supabase";
    })
    .AddCookie("Supabase", options =>
    {
        options.LoginPath = "/";
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, null);

builder.Services.AddAuthorization(options =>
{
    // Add a policy that requires the ApiKey scheme
    options.AddPolicy("ApiKeyPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
        policy.RequireAuthenticatedUser();
    });
});
builder.Services.AddCascadingAuthenticationState();
// Configure Supabase Client
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
builder.Services.AddScoped(provider => new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
}));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connStr,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "OpenMods API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API key needed to access the endpoints. Format: X-API-Key: YOUR_KEY",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    var scheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    };
    var requirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { scheme, new System.Collections.Generic.List<string>() }
    };
    c.AddSecurityRequirement(requirement);
});
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

// Enforce HTTPS redirection
app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
