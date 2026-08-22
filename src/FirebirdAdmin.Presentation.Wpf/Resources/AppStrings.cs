using System.Globalization;
using System.Resources;

namespace FirebirdAdmin.Presentation.Wpf.Resources;

public static class AppStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "FirebirdAdmin.Presentation.Wpf.Resources.AppStrings",
        typeof(AppStrings).Assembly);

    public static string AppName => GetString(nameof(AppName));
    public static string ConnectionContextEmpty => GetString(nameof(ConnectionContextEmpty));
    public static string Dashboard => GetString(nameof(Dashboard));
    public static string Diagnostics => GetString(nameof(Diagnostics));
    public static string History => GetString(nameof(History));
    public static string Maintenance => GetString(nameof(Maintenance));
    public static string Metadata => GetString(nameof(Metadata));
    public static string Monitoring => GetString(nameof(Monitoring));
    public static string NavigationTitle => GetString(nameof(NavigationTitle));
    public static string PollingStopped => GetString(nameof(PollingStopped));
    public static string ReadyStatus => GetString(nameof(ReadyStatus));
    public static string Settings => GetString(nameof(Settings));
    public static string SqlProfiler => GetString(nameof(SqlProfiler));
    public static string TraceStopped => GetString(nameof(TraceStopped));
    public static string WorkspacePlaceholder => GetString(nameof(WorkspacePlaceholder));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
