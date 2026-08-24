using FirebirdAdmin.Presentation.Wpf.Resources;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class ResourceTests
{
    [Fact]
    public void AppStrings_ShouldResolveVisibleShellStrings()
    {
        AppStrings.AppName.Should().Be("Firebird Admin");
        AppStrings.ConnectionContextEmpty.Should().NotBeNullOrWhiteSpace();
        AppStrings.Connect.Should().Be("Conectar");
        AppStrings.TestConnection.Should().NotBeNullOrWhiteSpace();
        AppStrings.DashboardOperationalTitle.Should().Be("Dashboard operacional");
        AppStrings.TransactionsTitle.Should().NotBeNullOrWhiteSpace();
        AppStrings.Start.Should().Be("Iniciar");
        AppStrings.KeyboardHelp.Should().Contain("Ctrl+1");
        AppStrings.WorkspacePlaceholder.Should().Contain("Sprint 1");
    }
}
