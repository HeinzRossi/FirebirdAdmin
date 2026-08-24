using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FirebirdAdmin.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
public sealed class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11");

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.ConnectionProfileEntity", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever();
            b.Property<string>("Charset").HasMaxLength(40);
            b.Property<DateTimeOffset>("CreatedAt");
            b.Property<string>("Database").IsRequired().HasMaxLength(1024);
            b.Property<string>("Host").IsRequired().HasMaxLength(255);
            b.Property<string>("Name").IsRequired().HasMaxLength(120);
            b.Property<int>("Port");
            b.Property<byte[]>("ProtectedPasswordBlob");
            b.Property<string>("Role").HasMaxLength(120);
            b.Property<DateTimeOffset>("UpdatedAt");
            b.Property<string>("UserName").IsRequired().HasMaxLength(120);
            b.HasKey("Id");
            b.HasIndex("Name").IsUnique();
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.HistoryRetentionPolicyEntity", b =>
        {
            b.Property<int>("Id").ValueGeneratedNever();
            b.Property<int>("BatchSize");
            b.Property<long>("MaxDatabaseBytes");
            b.Property<int>("RetentionDays");
            b.HasKey("Id");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.AlertEventEntity", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever();
            b.Property<string>("AcknowledgementNote");
            b.Property<string>("CorrelationKey").IsRequired();
            b.Property<string>("EvidenceJson").IsRequired();
            b.Property<DateTimeOffset>("FirstSeen");
            b.Property<DateTimeOffset>("LastSeen");
            b.Property<string>("Message").IsRequired();
            b.Property<int>("Occurrences");
            b.Property<string>("RuleId").IsRequired();
            b.Property<string>("Severity").IsRequired();
            b.Property<string>("Status").IsRequired();
            b.Property<string>("TargetDisplayName");
            b.Property<string>("TargetId").IsRequired();
            b.Property<string>("TargetType").IsRequired();
            b.HasKey("Id");
            b.HasIndex("CorrelationKey").IsUnique();
            b.HasIndex("LastSeen");
            b.HasIndex("Severity");
            b.HasIndex("Status");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.AlertNotificationEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<Guid>("AlertId");
            b.Property<string>("Channel").IsRequired();
            b.Property<string>("Message").IsRequired();
            b.Property<DateTimeOffset>("SentAt");
            b.HasKey("Id");
            b.HasIndex("AlertId");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.MaintenanceOperationEntity", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever();
            b.Property<Guid?>("ConnectionProfileId");
            b.Property<int>("ExitCode");
            b.Property<DateTimeOffset?>("FinishedAt");
            b.Property<string>("Message").IsRequired();
            b.Property<string>("Source").IsRequired();
            b.Property<DateTimeOffset>("StartedAt");
            b.Property<string>("Status").IsRequired();
            b.Property<string>("Target");
            b.Property<string>("Type").IsRequired();
            b.HasKey("Id");
            b.HasIndex("StartedAt");
            b.HasIndex("Status");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.MaintenanceOperationLogEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<Guid>("OperationId");
            b.Property<string>("Stream").IsRequired();
            b.Property<string>("Text").IsRequired();
            b.Property<DateTimeOffset>("Timestamp");
            b.HasKey("Id");
            b.HasIndex("OperationId");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.MonitoringSessionEntity", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever();
            b.Property<Guid?>("ConnectionProfileId");
            b.Property<bool>("IsProtected");
            b.Property<string>("Kind").IsRequired();
            b.Property<DateTimeOffset>("StartedAt");
            b.Property<DateTimeOffset?>("StoppedAt");
            b.HasKey("Id");
            b.HasIndex("StartedAt");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.MonitoringSnapshotEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<int>("AttachmentCount");
            b.Property<DateTimeOffset>("CapturedAt");
            b.Property<Guid?>("ConnectionProfileId");
            b.Property<Guid>("SessionId");
            b.Property<int>("StatementCount");
            b.Property<int>("TransactionCount");
            b.HasKey("Id");
            b.HasIndex("CapturedAt");
            b.HasIndex("ConnectionProfileId");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.PerformanceSnapshotEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<int>("AttachmentCount");
            b.Property<DateTimeOffset>("CapturedAt");
            b.Property<Guid?>("ConnectionProfileId");
            b.Property<int>("StatementCount");
            b.Property<int>("TransactionCount");
            b.HasKey("Id");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.StatementExecutionEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<Guid?>("ConnectionProfileId");
            b.Property<double?>("DurationMs");
            b.Property<string>("Sql");
            b.Property<DateTimeOffset>("Timestamp");
            b.Property<long>("TraceEventId");
            b.HasKey("Id");
        });

        modelBuilder.Entity("FirebirdAdmin.Infrastructure.Persistence.Entities.TraceEventEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<long?>("AttachmentId");
            b.Property<Guid?>("ConnectionProfileId");
            b.Property<double?>("DurationMs");
            b.Property<long?>("Fetches");
            b.Property<long?>("Marks");
            b.Property<string>("Plan");
            b.Property<string>("RawTrace").IsRequired();
            b.Property<long?>("Reads");
            b.Property<long>("Sequence");
            b.Property<string>("Sql");
            b.Property<DateTimeOffset>("Timestamp");
            b.Property<long?>("TransactionId");
            b.Property<string>("Type").IsRequired();
            b.Property<string>("UserName");
            b.Property<long?>("Writes");
            b.HasKey("Id");
            b.HasIndex("Timestamp");
            b.HasIndex("ConnectionProfileId");
            b.HasIndex("UserName");
            b.HasIndex("AttachmentId");
            b.HasIndex("TransactionId");
        });
    }
}
