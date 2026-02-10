using Microsoft.EntityFrameworkCore;
using OpenMods.Shared.Models;

namespace OpenMods.Shared.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrEmpty(connStr))
            {
                optionsBuilder.UseNpgsql(connStr);
            }
        }
    }

    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<Mod> Mods => Set<Mod>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<ReleaseAsset> ReleaseAssets => Set<ReleaseAsset>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<DailyMetric> DailyMetrics => Set<DailyMetric>();
    public DbSet<ModLink> ModLinks => Set<ModLink>();
    public DbSet<ModFaq> ModFaqs => Set<ModFaq>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Developer>()
            .HasMany(d => d.Mods)
            .WithOne(m => m.Developer)
            .HasForeignKey(m => m.DeveloperId);

        modelBuilder.Entity<Developer>()
            .HasMany(d => d.ApiKeys)
            .WithOne(a => a.Developer)
            .HasForeignKey(a => a.DeveloperId);

        modelBuilder.Entity<Mod>()
            .HasMany(m => m.Releases)
            .WithOne(r => r.Mod)
            .HasForeignKey(r => r.ModId);

        modelBuilder.Entity<Release>()
            .HasMany(r => r.SupportedGames)
            .WithMany(g => g.Releases);

        modelBuilder.Entity<Mod>()
            .HasMany(m => m.DailyMetrics)
            .WithOne(dm => dm.Mod)
            .HasForeignKey(dm => dm.ModId);

        modelBuilder.Entity<Mod>()
            .HasMany(m => m.SupportedGames)
            .WithMany(g => g.Mods);

        modelBuilder.Entity<Mod>()
            .HasMany(m => m.ExternalLinks)
            .WithOne()
            .HasForeignKey(l => l.ModId);

        modelBuilder.Entity<Game>().HasData(
            new Game { Id = 1, Name = "Aether Protocol", ImageUrl = "https://images.unsplash.com/photo-1582125032515-3850559e3549?auto=format&fit=crop&q=80&w=800", Description = "Sci-fi Sandbox" },
            new Game { Id = 2, Name = "Neon Horizon", ImageUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?auto=format&fit=crop&q=80&w=800", Description = "Cyberpunk RPG" },
            new Game { Id = 3, Name = "Code Strider", ImageUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&q=80&w=800", Description = "Neural Space Simulation" },
            new Game { Id = 4, Name = "The Flux Engine", ImageUrl = "https://images.unsplash.com/photo-1605810230434-7631ac76ec81?auto=format&fit=crop&q=80&w=800", Description = "Abstract Engine" },
            new Game { Id = 5, Name = "Zero Point", ImageUrl = "https://images.unsplash.com/photo-1614850523459-c2f4c699c52e?auto=format&fit=crop&q=80&w=800", Description = "Void Exploration" },
            new Game { Id = 6, Name = "Rift Walkers", ImageUrl = "https://images.unsplash.com/photo-1538370910019-0d1052bd7729?auto=format&fit=crop&q=80&w=800", Description = "Tactical Dimension RPG" },
            new Game { Id = 7, Name = "Shadow Script", ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&q=80&w=800", Description = "Minimalist Stealth" },
            new Game { Id = 8, Name = "Binary Outpost", ImageUrl = "https://images.unsplash.com/photo-1581092160562-40aa08e78837?auto=format&fit=crop&q=80&w=800", Description = "Construction Simulation" },
            new Game { Id = 9, Name = "System Shockwaves", ImageUrl = "https://images.unsplash.com/photo-1517077304055-6e89abbf09b0?auto=format&fit=crop&q=80&w=800", Description = "Retro Futuristic FPS" },
            new Game { Id = 10, Name = "Grid Runner", ImageUrl = "https://images.unsplash.com/photo-1558591710-4b4a1ae0f04d?auto=format&fit=crop&q=80&w=800", Description = "Data Flow Racing" }
        );
    }
}
