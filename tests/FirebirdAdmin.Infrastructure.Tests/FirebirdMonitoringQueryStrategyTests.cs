using FirebirdAdmin.Infrastructure.Monitoring;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FirebirdMonitoringQueryStrategyTests
{
    [Fact]
    public void GetRequiredColumns_ShouldDocumentBaselineMonFields()
    {
        var columns = FirebirdMonitoringQueryStrategy.GetRequiredColumns();

        columns["MON$ATTACHMENTS"].Should().Contain(["MON$ATTACHMENT_ID", "MON$USER", "MON$TIMESTAMP"]);
        columns["MON$TRANSACTIONS"].Should().Contain(["MON$TRANSACTION_ID", "MON$OLDEST_ACTIVE", "MON$LOCK_TIMEOUT"]);
        columns["MON$STATEMENTS"].Should().Contain(["MON$STATEMENT_ID", "MON$SQL_TEXT"]);
    }
}
