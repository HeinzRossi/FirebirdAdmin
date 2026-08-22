using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class TransactionsWorkspaceViewModelTests
{
    [Fact]
    public void ApplySnapshot_ShouldDiffRowsAndPreserveSelection()
    {
        var viewModel = new TransactionsWorkspaceViewModel();
        var first = new MonitoringSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [],
            [
                new TransactionSnapshot(1, 10, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4),
                new TransactionSnapshot(2, 20, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4)
            ],
            []);

        viewModel.ApplySnapshot(first);
        viewModel.SelectedTransaction = viewModel.Transactions.Single(row => row.TransactionId == 2);

        var second = first with
        {
            Transactions =
            [
                new TransactionSnapshot(2, 25, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4),
                new TransactionSnapshot(3, 30, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4)
            ]
        };

        viewModel.ApplySnapshot(second);

        viewModel.Transactions.Select(row => row.TransactionId).Should().Equal(2, 3);
        viewModel.SelectedTransaction.Should().NotBeNull();
        viewModel.SelectedTransaction!.TransactionId.Should().Be(2);
        viewModel.SelectedTransaction.AttachmentId.Should().Be(25);
        viewModel.State.Should().Be(TransactionsWorkspaceState.Ready);
    }
}
