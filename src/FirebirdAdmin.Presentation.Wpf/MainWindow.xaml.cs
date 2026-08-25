using FirebirdAdmin.Presentation.Wpf.Shell;
using FirebirdAdmin.Presentation.Wpf.Resources;
using ScottPlot;
using ScottPlot.WPF;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows;

namespace FirebirdAdmin.Presentation.Wpf;

public partial class MainWindow
{
    private readonly ShellViewModel viewModel;
    private WpfPlot? activityPlot;
    private PasswordBox? passwordInput;
    private Rect? restoreBoundsBeforeOperationalMaximize;
    private bool isOperationalMaximized;

    public MainWindow(ShellViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Dashboard.ActivityChanged += Dashboard_OnActivityChanged;
        UpdateActivityPlot();
        UpdateMaximizeRestoreGlyph();
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

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

        if (WindowState == WindowState.Normal && !isOperationalMaximized)
        {
            DragMove();
            e.Handled = true;
        }
    }

    private void MinimizeWindowButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeRestoreWindowButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseWindowButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }

    private void Window_OnStateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeRestoreGlyph();
    }

    private void Window_OnSourceInitialized(object? sender, EventArgs e)
    {
        var workArea = GetCurrentMonitorWorkArea();
        restoreBoundsBeforeOperationalMaximize = CreateCenteredRestoreBounds(workArea);
        ApplyOperationalMaximize(rememberCurrentBounds: false);
    }

    private void ToggleMaximizeRestore()
    {
        if (isOperationalMaximized)
        {
            RestoreOperationalBounds();
        }
        else
        {
            ApplyOperationalMaximize(rememberCurrentBounds: true);
        }

        UpdateMaximizeRestoreGlyph();
    }

    private void UpdateMaximizeRestoreGlyph()
    {
        if (MaximizeRestoreGlyph is null)
        {
            return;
        }

        MaximizeRestoreGlyph.Text = isOperationalMaximized ? "\uE923" : "\uE922";
    }

    private void ApplyOperationalMaximize(bool rememberCurrentBounds)
    {
        if (rememberCurrentBounds && WindowState == WindowState.Normal && !isOperationalMaximized)
        {
            restoreBoundsBeforeOperationalMaximize = new Rect(Left, Top, Width, Height);
        }

        if (WindowState != WindowState.Normal)
        {
            WindowState = WindowState.Normal;
        }

        var workArea = GetCurrentMonitorWorkArea();
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
        isOperationalMaximized = true;
        UpdateMaximizeRestoreGlyph();
    }

    private void RestoreOperationalBounds()
    {
        if (WindowState != WindowState.Normal)
        {
            WindowState = WindowState.Normal;
        }

        var restoreBounds = restoreBoundsBeforeOperationalMaximize ?? CreateCenteredRestoreBounds(GetCurrentMonitorWorkArea());
        Left = restoreBounds.Left;
        Top = restoreBounds.Top;
        Width = Math.Max(MinWidth, restoreBounds.Width);
        Height = Math.Max(MinHeight, restoreBounds.Height);
        isOperationalMaximized = false;
        UpdateMaximizeRestoreGlyph();
    }

    private Rect CreateCenteredRestoreBounds(Rect workArea)
    {
        var width = Math.Min(Math.Max(MinWidth, Width), workArea.Width);
        var height = Math.Min(Math.Max(MinHeight, Height), workArea.Height);
        var left = workArea.Left + Math.Max(0, (workArea.Width - width) / 2);
        var top = workArea.Top + Math.Max(0, (workArea.Height - height) / 2);
        return new Rect(left, top, width, height);
    }

    private Rect GetCurrentMonitorWorkArea()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };

        if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
        {
            return DeviceRectToDip(monitorInfo.WorkArea);
        }

        return SystemParameters.WorkArea;
    }

    private Rect DeviceRectToDip(NativeRect rect)
    {
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new Point(rect.Left, rect.Top));
        var bottomRight = transform.Transform(new Point(rect.Right, rect.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private const int MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
