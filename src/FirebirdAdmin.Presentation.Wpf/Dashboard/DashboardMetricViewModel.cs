using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Dashboard;

namespace FirebirdAdmin.Presentation.Wpf.Dashboard;

public sealed partial class DashboardMetricViewModel : ObservableObject
{
    [ObservableProperty]
    private string key;

    [ObservableProperty]
    private string label;

    [ObservableProperty]
    private string value;

    [ObservableProperty]
    private string? detail;

    public DashboardMetricViewModel(DashboardMetric metric)
    {
        key = metric.Key;
        label = metric.Label;
        value = metric.Value;
        detail = metric.Detail;
    }

    public void Apply(DashboardMetric metric)
    {
        Label = metric.Label;
        Value = metric.Value;
        Detail = metric.Detail;
    }
}
