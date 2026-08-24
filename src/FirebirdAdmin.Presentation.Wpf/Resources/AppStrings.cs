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
    public static string DashboardOperationalTitle => GetString(nameof(DashboardOperationalTitle));
    public static string Diagnostics => GetString(nameof(Diagnostics));
    public static string History => GetString(nameof(History));
    public static string Host => GetString(nameof(Host));
    public static string Maintenance => GetString(nameof(Maintenance));
    public static string Metadata => GetString(nameof(Metadata));
    public static string Monitoring => GetString(nameof(Monitoring));
    public static string TransactionsTitle => GetString(nameof(TransactionsTitle));
    public static string TransactionDetailsTitle => GetString(nameof(TransactionDetailsTitle));
    public static string TransactionsFilterLabel => GetString(nameof(TransactionsFilterLabel));
    public static string Name => GetString(nameof(Name));
    public static string NavigationTitle => GetString(nameof(NavigationTitle));
    public static string Password => GetString(nameof(Password));
    public static string PollingStopped => GetString(nameof(PollingStopped));
    public static string Port => GetString(nameof(Port));
    public static string ReadyStatus => GetString(nameof(ReadyStatus));
    public static string RememberPassword => GetString(nameof(RememberPassword));
    public static string Role => GetString(nameof(Role));
    public static string SaveProfile => GetString(nameof(SaveProfile));
    public static string Start => GetString(nameof(Start));
    public static string PauseView => GetString(nameof(PauseView));
    public static string Follow => GetString(nameof(Follow));
    public static string Stop => GetString(nameof(Stop));
    public static string Clear => GetString(nameof(Clear));
    public static string Search => GetString(nameof(Search));
    public static string Refresh => GetString(nameof(Refresh));
    public static string Acknowledge => GetString(nameof(Acknowledge));
    public static string Resolve => GetString(nameof(Resolve));
    public static string Reopen => GetString(nameof(Reopen));
    public static string Load => GetString(nameof(Load));
    public static string Object => GetString(nameof(Object));
    public static string Back => GetString(nameof(Back));
    public static string Forward => GetString(nameof(Forward));
    public static string MarkStale => GetString(nameof(MarkStale));
    public static string Confirm => GetString(nameof(Confirm));
    public static string Validate => GetString(nameof(Validate));
    public static string Execute => GetString(nameof(Execute));
    public static string Cancel => GetString(nameof(Cancel));
    public static string UpdateHistory => GetString(nameof(UpdateHistory));
    public static string AlertsInstruction => GetString(nameof(AlertsInstruction));
    public static string KeyboardHelp => GetString(nameof(KeyboardHelp));
    public static string TestConnectionSucceeded => GetString(nameof(TestConnectionSucceeded));
    public static string Settings => GetString(nameof(Settings));
    public static string Security => GetString(nameof(Security));
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
