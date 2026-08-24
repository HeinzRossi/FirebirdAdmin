using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using FirebirdAdmin.Infrastructure.Persistence;

#nullable disable

namespace FirebirdAdmin.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824000000_InitialHistorySchema")]
public partial class InitialHistorySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "ConnectionProfiles" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ConnectionProfiles" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Host" TEXT NOT NULL,
                "Port" INTEGER NOT NULL,
                "Database" TEXT NOT NULL,
                "UserName" TEXT NOT NULL,
                "Charset" TEXT NULL,
                "Role" TEXT NULL,
                "ProtectedPasswordBlob" BLOB NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "MonitoringSessions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_MonitoringSessions" PRIMARY KEY,
                "ConnectionProfileId" TEXT NULL,
                "Kind" TEXT NOT NULL,
                "StartedAt" TEXT NOT NULL,
                "StoppedAt" TEXT NULL,
                "IsProtected" INTEGER NOT NULL DEFAULT 0
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "MonitoringSnapshots" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MonitoringSnapshots" PRIMARY KEY AUTOINCREMENT,
                "ConnectionProfileId" TEXT NULL,
                "SessionId" TEXT NOT NULL,
                "CapturedAt" TEXT NOT NULL,
                "AttachmentCount" INTEGER NOT NULL,
                "TransactionCount" INTEGER NOT NULL,
                "StatementCount" INTEGER NOT NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "TraceEvents" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_TraceEvents" PRIMARY KEY AUTOINCREMENT,
                "ConnectionProfileId" TEXT NULL,
                "Sequence" INTEGER NOT NULL,
                "Timestamp" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "DurationMs" REAL NULL,
                "UserName" TEXT NULL,
                "AttachmentId" INTEGER NULL,
                "TransactionId" INTEGER NULL,
                "Sql" TEXT NULL,
                "Reads" INTEGER NULL,
                "Writes" INTEGER NULL,
                "Fetches" INTEGER NULL,
                "Marks" INTEGER NULL,
                "Plan" TEXT NULL,
                "RawTrace" TEXT NOT NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "StatementExecutions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_StatementExecutions" PRIMARY KEY AUTOINCREMENT,
                "ConnectionProfileId" TEXT NULL,
                "TraceEventId" INTEGER NOT NULL,
                "Timestamp" TEXT NOT NULL,
                "Sql" TEXT NULL,
                "DurationMs" REAL NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "PerformanceSnapshots" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PerformanceSnapshots" PRIMARY KEY AUTOINCREMENT,
                "ConnectionProfileId" TEXT NULL,
                "CapturedAt" TEXT NOT NULL,
                "AttachmentCount" INTEGER NOT NULL,
                "TransactionCount" INTEGER NOT NULL,
                "StatementCount" INTEGER NOT NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "HistoryRetentionPolicies" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_HistoryRetentionPolicies" PRIMARY KEY,
                "RetentionDays" INTEGER NOT NULL,
                "MaxDatabaseBytes" INTEGER NOT NULL,
                "BatchSize" INTEGER NOT NULL
            );
            """);

        migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConnectionProfiles_Name" ON "ConnectionProfiles" ("Name");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_MonitoringSessions_StartedAt" ON "MonitoringSessions" ("StartedAt");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_MonitoringSnapshots_CapturedAt" ON "MonitoringSnapshots" ("CapturedAt");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_MonitoringSnapshots_ConnectionProfileId" ON "MonitoringSnapshots" ("ConnectionProfileId");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TraceEvents_Timestamp" ON "TraceEvents" ("Timestamp");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TraceEvents_ConnectionProfileId" ON "TraceEvents" ("ConnectionProfileId");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TraceEvents_UserName" ON "TraceEvents" ("UserName");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TraceEvents_AttachmentId" ON "TraceEvents" ("AttachmentId");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TraceEvents_TransactionId" ON "TraceEvents" ("TransactionId");""");
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "HistoryRetentionPolicies" ("Id", "RetentionDays", "MaxDatabaseBytes", "BatchSize")
            VALUES (1, 30, 5368709120, 500);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("HistoryRetentionPolicies");
        migrationBuilder.DropTable("PerformanceSnapshots");
        migrationBuilder.DropTable("StatementExecutions");
        migrationBuilder.DropTable("TraceEvents");
        migrationBuilder.DropTable("MonitoringSnapshots");
        migrationBuilder.DropTable("MonitoringSessions");
    }
}
