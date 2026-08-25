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
        AppStrings.PasswordSavedForProfile.Should().Contain("salva");
        AppStrings.PasswordNotSavedForProfile.Should().Contain("não salva");
        AppStrings.PasswordUnavailable.Should().Contain("senha");
        AppStrings.PasswordRequired.Should().Be("Informe a senha para conectar.");
        AppStrings.PasswordForgottenForProfile.Should().Contain("removida");
        AppStrings.DashboardOperationalTitle.Should().Be("Dashboard operacional");
        AppStrings.TransactionsTitle.Should().NotBeNullOrWhiteSpace();
        AppStrings.Start.Should().Be("Iniciar");
        AppStrings.ResumeView.Should().Be("Continuar visualização");
        AppStrings.ColumnTransaction.Should().Be("Transaction");
        AppStrings.ColumnSeverity.Should().Be("Sev");
        AppStrings.TabEvidence.Should().Be("Evidências");
        AppStrings.LastUpdatedFormat.Should().Contain("{0}");
        AppStrings.ExportCsv.Should().Be("CSV");
        AppStrings.Exit.Should().Be("Sair");
        AppStrings.About.Should().Be("Sobre");
        AppStrings.AboutTitle.Should().Contain("Firebird Admin");
        AppStrings.AboutVersionFormat.Should().Contain("{0}");
        AppStrings.ThemeLight.Should().Be("Claro");
        AppStrings.ThemeDark.Should().Be("Escuro");
        AppStrings.ThemeToggleFormat.Should().Contain("{0}");
        AppStrings.RefreshObject.Should().Be("Atualizar objeto");
        AppStrings.HistoryDataKindTraceEvents.Should().Be("Eventos Trace");
        AppStrings.HistoryDataKindMonitoringSnapshots.Should().Be("Snapshots de monitoramento");
        AppStrings.MaintenanceOperationValidation.Should().Be("Validação");
        AppStrings.MaintenanceSourceDatabase.Should().Be("Banco");
        AppStrings.MaintenanceSourceBackup.Should().Be("Backup");
        AppStrings.MaintenanceTargetBackup.Should().Be("Arquivo backup");
        AppStrings.MaintenanceTargetNewDatabase.Should().Be("Novo banco");
        AppStrings.MaintenanceTargetNotUsed.Should().Be("Destino não usado");
        AppStrings.FilterAllStatus.Should().Be("Todos");
        AppStrings.FilterStatusActive.Should().Be("Ativos");
        AppStrings.FilterStatusAcknowledged.Should().Be("Reconhecidos");
        AppStrings.FilterStatusResolved.Should().Be("Resolvidos");
        AppStrings.FilterAllSeverities.Should().Be("Todas");
        AppStrings.FilterSeverityCritical.Should().Be("Crítica");
        AppStrings.FilterSeverityHigh.Should().Be("Alta");
        AppStrings.FilterSeverityMedium.Should().Be("Média");
        AppStrings.FilterSeverityLow.Should().Be("Baixa");
        AppStrings.FilterSeverityInfo.Should().Be("Info");
        AppStrings.MaintenanceProgressWaiting.Should().Contain("Aguardando");
        AppStrings.MaintenanceProgressRunning.Should().Contain("execução");
        AppStrings.MaintenanceProgressCompleted.Should().Contain("concluída");
        AppStrings.MaintenanceProgressCancelled.Should().Contain("cancelada");
        AppStrings.MaintenanceProgressFailed.Should().Contain("falhou");
        AppStrings.KeyboardHelp.Should().Contain("Ctrl+1");
        AppStrings.WorkspacePlaceholder.Should().Contain("Conecte");
    }

    [Fact]
    public void FilterOption_ShouldRenderLabelOnly()
    {
        new FirebirdAdmin.Presentation.Wpf.Diagnostics.FilterOption("Sweep", "Sweep").ToString().Should().Be("Sweep");
        new FirebirdAdmin.Presentation.Wpf.Diagnostics.FilterOption("Validação", "Validation").ToString().Should().Be("Validação");
    }
}
