using FirebirdAdmin.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirebirdAdmin.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ConnectionProfileEntity> ConnectionProfiles => Set<ConnectionProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<ConnectionProfileEntity>();
        profile.HasKey(entity => entity.Id);
        profile.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        profile.Property(entity => entity.Host).HasMaxLength(255).IsRequired();
        profile.Property(entity => entity.Database).HasMaxLength(1024).IsRequired();
        profile.Property(entity => entity.UserName).HasMaxLength(120).IsRequired();
        profile.Property(entity => entity.Charset).HasMaxLength(40);
        profile.Property(entity => entity.Role).HasMaxLength(120);
        profile.HasIndex(entity => entity.Name).IsUnique();
    }
}
