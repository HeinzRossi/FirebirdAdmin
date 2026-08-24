using FirebirdAdmin.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirebirdAdmin.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ConnectionProfileEntity> ConnectionProfiles => Set<ConnectionProfileEntity>();
    public DbSet<MonitoringSessionEntity> MonitoringSessions => Set<MonitoringSessionEntity>();
    public DbSet<MonitoringSnapshotEntity> MonitoringSnapshots => Set<MonitoringSnapshotEntity>();
    public DbSet<TraceEventEntity> TraceEvents => Set<TraceEventEntity>();
    public DbSet<StatementExecutionEntity> StatementExecutions => Set<StatementExecutionEntity>();
    public DbSet<PerformanceSnapshotEntity> PerformanceSnapshots => Set<PerformanceSnapshotEntity>();
    public DbSet<HistoryRetentionPolicyEntity> HistoryRetentionPolicies => Set<HistoryRetentionPolicyEntity>();

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

        modelBuilder.Entity<MonitoringSessionEntity>().HasKey(entity => entity.Id);
        modelBuilder.Entity<MonitoringSessionEntity>().HasIndex(entity => entity.StartedAt);
        modelBuilder.Entity<MonitoringSnapshotEntity>().HasKey(entity => entity.Id);
        modelBuilder.Entity<MonitoringSnapshotEntity>().HasIndex(entity => entity.CapturedAt);
        modelBuilder.Entity<MonitoringSnapshotEntity>().HasIndex(entity => entity.ConnectionProfileId);
        modelBuilder.Entity<TraceEventEntity>().HasKey(entity => entity.Id);
        modelBuilder.Entity<TraceEventEntity>().HasIndex(entity => entity.Timestamp);
        modelBuilder.Entity<TraceEventEntity>().HasIndex(entity => entity.ConnectionProfileId);
        modelBuilder.Entity<TraceEventEntity>().HasIndex(entity => entity.UserName);
        modelBuilder.Entity<TraceEventEntity>().HasIndex(entity => entity.AttachmentId);
        modelBuilder.Entity<TraceEventEntity>().HasIndex(entity => entity.TransactionId);
        modelBuilder.Entity<StatementExecutionEntity>().HasKey(entity => entity.Id);
        modelBuilder.Entity<PerformanceSnapshotEntity>().HasKey(entity => entity.Id);
        modelBuilder.Entity<HistoryRetentionPolicyEntity>().HasKey(entity => entity.Id);
    }
}
