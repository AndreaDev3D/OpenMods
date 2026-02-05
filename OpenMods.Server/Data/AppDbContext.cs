using Microsoft.EntityFrameworkCore;
using OpenMods.Server.Models;

namespace OpenMods.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<Mod> Mods => Set<Mod>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Developer>()
            .HasMany(d => d.Mods)
            .WithOne(m => m.Developer)
            .HasForeignKey(m => m.DeveloperId);

        modelBuilder.Entity<Mod>()
            .HasMany(m => m.Releases)
            .WithOne(r => r.Mod)
            .HasForeignKey(r => r.ModId);
    }
}
