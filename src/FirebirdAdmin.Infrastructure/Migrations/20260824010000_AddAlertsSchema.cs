using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirebirdAdmin.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824010000_AddAlertsSchema")]
public partial class AddAlertsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "AlertEvents" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AlertEvents" PRIMARY KEY,
                "RuleId" TEXT NOT NULL,
                "CorrelationKey" TEXT NOT NULL,
                "Severity" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "TargetType" TEXT NOT NULL,
                "TargetId" TEXT NOT NULL,
                "TargetDisplayName" TEXT NULL,
                "FirstSeen" TEXT NOT NULL,
                "LastSeen" TEXT NOT NULL,
                "Occurrences" INTEGER NOT NULL,
                "EvidenceJson" TEXT NOT NULL,
                "AcknowledgementNote" TEXT NULL
            );
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "AlertNotifications" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AlertNotifications" PRIMARY KEY AUTOINCREMENT,
                "AlertId" TEXT NOT NULL,
                "Channel" TEXT NOT NULL,
                "SentAt" TEXT NOT NULL,
                "Message" TEXT NOT NULL
            );
            """);

        migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_AlertEvents_CorrelationKey" ON "AlertEvents" ("CorrelationKey");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_AlertEvents_Status" ON "AlertEvents" ("Status");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_AlertEvents_Severity" ON "AlertEvents" ("Severity");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_AlertEvents_LastSeen" ON "AlertEvents" ("LastSeen");""");
        migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_AlertNotifications_AlertId" ON "AlertNotifications" ("AlertId");""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AlertNotifications");
        migrationBuilder.DropTable("AlertEvents");
    }
}
