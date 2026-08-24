namespace FirebirdAdmin.Infrastructure.Persistence;

public sealed class ApplicationDataPaths
{
    public string RootDirectory { get; }
    public string DatabasePath { get; }
    public string BackupDirectory { get; }
    public string ExportDirectory { get; }

    public ApplicationDataPaths()
        : this(Path.Combine(GetLocalApplicationDataDirectory(), "FirebirdAdmin"))
    {
    }

    public ApplicationDataPaths(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        DatabasePath = Path.Combine(rootDirectory, "firebird-admin.db");
        BackupDirectory = Path.Combine(rootDirectory, "Backups");
        ExportDirectory = Path.Combine(rootDirectory, "Exports");
    }

    private static string GetLocalApplicationDataDirectory()
    {
        var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            return directory;
        }

        directory = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            return directory;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData",
            "Local");
    }
}
