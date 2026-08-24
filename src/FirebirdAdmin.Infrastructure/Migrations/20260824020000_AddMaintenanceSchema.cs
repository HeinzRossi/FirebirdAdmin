using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirebirdAdmin.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824020000_AddMaintenanceSchema")]
public partial class AddMaintenanceSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "MaintenanceOperations" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_MaintenanceOperations" PRIMARY KEY,
                "ConnectionProfileId" TEXT NULL,
                "Type" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "Source" TEXT NOT NULL,
                "Target" TEXT NULL,
                "StartedAt" TEXT NOT NULL,
                "FinishedAt" TEXT NULL,
                "ExitCode" INTEGER NOT NULL,
                "Message" TEXT NOT NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "MaintenanceOperationLogs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MaintenanceOperationLogs" PRIMARY KEY AUTOINCREMENT,
                "OperationId" TEXT NOT NULL,
                "Timestamp" TEXT NOT NULL,
                "Stream" TEXT NOT NULL,
                "Text" TEXT NOT NULL
            );
            """);

        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_MaintenanceOperations_StartedAt" ON "MaintenanceOperations" ("StartedAt");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_MaintenanceOperations_Status" ON "MaintenanceOperations" ("Status");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_MaintenanceOperationLogs_OperationId" ON "MaintenanceOperationLogs" ("OperationId");""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("MaintenanceOperationLogs");
        migrationBuilder.DropTable("MaintenanceOperations");
    }
}
