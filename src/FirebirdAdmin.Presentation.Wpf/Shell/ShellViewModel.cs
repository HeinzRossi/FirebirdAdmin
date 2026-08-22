using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Presentation.Wpf.Resources;

namespace FirebirdAdmin.Presentation.Wpf.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    public ShellViewModel()
    {
        NavigationItems =
        [
            new(AppStrings.Dashboard),
            new(AppStrings.Monitoring),
            new(AppStrings.SqlProfiler),
            new(AppStrings.Diagnostics),
            new(AppStrings.Metadata),
            new(AppStrings.Maintenance),
            new(AppStrings.History),
            new(AppStrings.Settings)
        ];
    }

    public string ApplicationName => AppStrings.AppName;
    public string NavigationTitle => AppStrings.NavigationTitle;
    public string ConnectionContext => AppStrings.ConnectionContextEmpty;
    public string ReadyStatus => AppStrings.ReadyStatus;
    public string TraceStatus => AppStrings.TraceStopped;
    public string PollingStatus => AppStrings.PollingStopped;
    public string WorkspaceTitle => AppStrings.Dashboard;
    public string WorkspacePlaceholder => AppStrings.WorkspacePlaceholder;
    public bool IsNavigationExpanded => true;
    public bool HasActiveConnection => false;
    public bool IsTraceRunning => false;
    public bool IsPollingRunning => false;
    public ObservableCollection<ShellNavigationItem> NavigationItems { get; }
}
