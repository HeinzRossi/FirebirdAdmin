using FirebirdAdmin.Presentation.Wpf.Shell;
using ScottPlot;

namespace FirebirdAdmin.Presentation.Wpf;

public partial class MainWindow
{
    private readonly ShellViewModel viewModel;

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
        if (ActivityPlot is null)
        {
            return;
        }

        ActivityPlot.Plot.Clear();
        var values = viewModel.Dashboard.GetActivityValues();
        if (values.Length > 0)
        {
            ActivityPlot.Plot.Add.Signal(values);
        }

        ActivityPlot.Plot.Axes.AutoScale();
        ActivityPlot.Refresh();
    }

    private async void SaveProfileButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SaveProfileAsync(PasswordInput.Password);
    }

    private async void TestConnectionButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.TestConnectionAsync(PasswordInput.Password);
    }

    private async void ConnectButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.ConnectAsync(PasswordInput.Password);
    }

    private async void StartProfilerButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.StartProfilerAsync(PasswordInput.Password);
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
}
