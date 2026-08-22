namespace FirebirdAdmin.Infrastructure.Persistence;

public sealed class ApplicationDataPaths
{
    public string RootDirectory { get; }
    public string DatabasePath { get; }

    public ApplicationDataPaths()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FirebirdAdmin"))
    {
    }

    public ApplicationDataPaths(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        DatabasePath = Path.Combine(rootDirectory, "firebird-admin.db");
    }
}
