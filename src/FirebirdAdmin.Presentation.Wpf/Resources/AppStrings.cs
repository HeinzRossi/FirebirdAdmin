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
    public static string Connect => GetString(nameof(Connect));
    public static string ConnectionFailed => GetString(nameof(ConnectionFailed));
    public static string ConnectionProfilesTitle => GetString(nameof(ConnectionProfilesTitle));
    public static string ConnectedStatus => GetString(nameof(ConnectedStatus));
    public static string ConnectingStatus => GetString(nameof(ConnectingStatus));
    public static string Database => GetString(nameof(Database));
    public static string Dashboard => GetString(nameof(Dashboard));
    public static string Diagnostics => GetString(nameof(Diagnostics));
    public static string History => GetString(nameof(History));
    public static string Host => GetString(nameof(Host));
    public static string Maintenance => GetString(nameof(Maintenance));
    public static string Metadata => GetString(nameof(Metadata));
    public static string Monitoring => GetString(nameof(Monitoring));
    public static string Name => GetString(nameof(Name));
    public static string NavigationTitle => GetString(nameof(NavigationTitle));
    public static string Password => GetString(nameof(Password));
    public static string PollingStopped => GetString(nameof(PollingStopped));
    public static string Port => GetString(nameof(Port));
    public static string ReadyStatus => GetString(nameof(ReadyStatus));
    public static string RememberPassword => GetString(nameof(RememberPassword));
    public static string Role => GetString(nameof(Role));
    public static string SaveProfile => GetString(nameof(SaveProfile));
    public static string Settings => GetString(nameof(Settings));
    public static string SqlProfiler => GetString(nameof(SqlProfiler));
    public static string TestConnection => GetString(nameof(TestConnection));
    public static string TraceStopped => GetString(nameof(TraceStopped));
    public static string UserName => GetString(nameof(UserName));
    public static string WorkspacePlaceholder => GetString(nameof(WorkspacePlaceholder));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
