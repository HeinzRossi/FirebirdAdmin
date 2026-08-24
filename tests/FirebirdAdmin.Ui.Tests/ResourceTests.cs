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
        AppStrings.SelectDatabase.Should().Be("Selecionar...");
        AppStrings.SelectDatabaseDialogTitle.Should().Be("Selecionar banco Firebird");
        AppStrings.DashboardOperationalTitle.Should().Be("Dashboard operacional");
        AppStrings.TransactionsTitle.Should().NotBeNullOrWhiteSpace();
        AppStrings.Start.Should().Be("Iniciar");
        AppStrings.ColumnTransaction.Should().Be("Transaction");
        AppStrings.ColumnSeverity.Should().Be("Sev");
        AppStrings.TabEvidence.Should().Be("Evidências");
        AppStrings.LastUpdatedFormat.Should().Contain("{0}");
        AppStrings.ExportCsv.Should().Be("CSV");
        AppStrings.Exit.Should().Be("Sair");
        AppStrings.ThemeLight.Should().Be("Claro");
        AppStrings.ThemeDark.Should().Be("Escuro");
        AppStrings.ThemeToggleFormat.Should().Contain("{0}");
        AppStrings.KeyboardHelp.Should().Contain("Ctrl+1");
        AppStrings.WorkspacePlaceholder.Should().Contain("Conecte");
    }
}
