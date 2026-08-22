using FirebirdAdmin.Presentation.Wpf.Resources;
using FirebirdAdmin.Presentation.Wpf.Shell;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class ShellViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeSprintZeroInitialState()
    {
        var viewModel = new ShellViewModel();

        viewModel.IsNavigationExpanded.Should().BeTrue();
        viewModel.HasActiveConnection.Should().BeFalse();
        viewModel.IsTraceRunning.Should().BeFalse();
        viewModel.IsPollingRunning.Should().BeFalse();
        viewModel.ReadyStatus.Should().Be(AppStrings.ReadyStatus);
        viewModel.TraceStatus.Should().Be(AppStrings.TraceStopped);
        viewModel.PollingStatus.Should().Be(AppStrings.PollingStopped);
        viewModel.NavigationItems.Select(item => item.Title).Should().Equal(
            AppStrings.Dashboard,
            AppStrings.Monitoring,
            AppStrings.SqlProfiler,
            AppStrings.Diagnostics,
            AppStrings.Metadata,
            AppStrings.Maintenance,
            AppStrings.History,
            AppStrings.Settings);
    }
}
