using FirebirdAdmin.Presentation.Wpf.Shell;
using FirebirdAdmin.Presentation.Wpf.Resources;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows.Controls;
using System.Windows.Input;

namespace FirebirdAdmin.Presentation.Wpf;

public partial class MainWindow
{
    private readonly ShellViewModel viewModel;
    private WpfPlot? activityPlot;
    private PasswordBox? passwordInput;

    public MainWindow(ShellViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Dashboard.ActivityChanged += Dashboard_OnActivityChanged;
        UpdateActivityPlot();
    }

    private void Dashboard_OnActivityChanged(object? sender, EventArgs e)
    {
        UpdateActivityPlot();
    }

    private void UpdateActivityPlot()
    {
        if (activityPlot is null)
        {
            return;
        }

        activityPlot.Plot.Clear();
        var values = viewModel.Dashboard.GetActivityValues();
        if (values.Length > 0)
        {
            activityPlot.Plot.Add.Signal(values);
        }

        activityPlot.Plot.Axes.AutoScale();
        activityPlot.Refresh();
    }

    private string CurrentPassword => passwordInput?.Password ?? string.Empty;

    private async void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
            e.Key is >= Key.D1 and <= Key.D9)
        {
            viewModel.SelectWorkspaceByShortcutCommand.Execute(((int)e.Key - (int)Key.D0).ToString());
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Enter)
        {
            if (viewModel.SelectedWorkspace == ShellWorkspace.Settings)
            {
                await viewModel.ConnectAsync(CurrentPassword);
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.F5)
        {
            await viewModel.RefreshSelectedWorkspaceAsync();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (viewModel.IsAboutOpen)
            {
                viewModel.CloseAbout();
                e.Handled = true;
                return;
            }

            viewModel.CancelCurrentWorkspaceAction();
            e.Handled = true;
        }
    }

    private void ActivityPlot_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        activityPlot = (WpfPlot)sender;
        UpdateActivityPlot();
    }

    private void ActivityPlot_OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (ReferenceEquals(activityPlot, sender))
        {
            activityPlot = null;
        }
    }

    private void PasswordInput_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        passwordInput = (PasswordBox)sender;
    }

    private void PasswordInput_OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (ReferenceEquals(passwordInput, sender))
        {
            passwordInput = null;
        }
    }

    private async void SaveProfileButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SaveProfileAsync(CurrentPassword);
    }

    private async void TestConnectionButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.TestConnectionAsync(CurrentPassword);
    }

    private async void ConnectButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.ConnectAsync(CurrentPassword);
    }

    private void SelectDatabaseButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = AppStrings.SelectDatabaseDialogTitle,
            Filter = "Bancos Firebird (*.fdb;*.gdb)|*.fdb;*.gdb|Todos os arquivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            viewModel.Database = dialog.FileName;
        }
    }

    private async void StartProfilerButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.StartProfilerAsync(CurrentPassword);
    }

    private async void StopProfilerButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.StopProfilerAsync();
    }

    private void PauseProfilerButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        viewModel.PauseProfilerView();
    }

    private void FollowProfilerButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        viewModel.ResumeProfilerFollow();
    }

    private void ClearProfilerButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        viewModel.ProfilerWorkspace.Clear();
    }

    private async void SearchHistoryButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.HistoryWorkspace.SearchAsync();
    }

    private async void ExportHistoryCsvButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.HistoryWorkspace.ExportCsvAsync();
    }

    private async void ExportHistoryJsonButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.HistoryWorkspace.ExportJsonAsync();
    }

    private async void RefreshAlertsButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.AlertsCenter.LoadAsync();
    }

    private async void AcknowledgeAlertButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.AlertsCenter.AcknowledgeAsync();
    }

    private async void ResolveAlertButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.AlertsCenter.ResolveAsync();
    }

    private async void ReopenAlertButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.AlertsCenter.ReopenAsync();
    }

    private async void LoadMetadataCatalogButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MetadataExplorer.LoadCatalogAsync();
    }

    private async void RefreshMetadataCatalogButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MetadataExplorer.RefreshCatalogAsync();
    }

    private async void RefreshMetadataObjectButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MetadataExplorer.RefreshSelectedAsync();
    }

    private async void MetadataBackButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MetadataExplorer.BackAsync();
    }

    private async void MetadataForwardButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MetadataExplorer.ForwardAsync();
    }

    private async void ValidateMaintenanceButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MaintenanceWorkspace.ValidateAsync();
    }

    private async void ExecuteMaintenanceButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MaintenanceWorkspace.ExecuteAsync();
    }

    private void CancelMaintenanceButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        viewModel.MaintenanceWorkspace.Cancel();
    }

    private async void RefreshMaintenanceHistoryButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.MaintenanceWorkspace.LoadHistoryAsync();
    }

    private async void LoadSecurityButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SecurityWorkspace.LoadAsync();
    }

    private async void RefreshSecurityButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SecurityWorkspace.RefreshAsync();
    }

    private void MarkSecurityStaleButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        viewModel.SecurityWorkspace.MarkStale();
    }

    private void ExitButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }
}
