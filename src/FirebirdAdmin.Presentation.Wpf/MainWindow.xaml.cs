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
}
