namespace FirebirdAdmin.Infrastructure.Tests;

internal sealed class TempDatabaseFixture : IDisposable
{
    private readonly TempDirectoryFixture directory;

    private TempDatabaseFixture(TempDirectoryFixture directory)
    {
        this.directory = directory;
        DatabasePath = Path.Combine(directory.Path, "firebird-admin-test.db");
    }

    public string DatabasePath { get; }

    public static TempDatabaseFixture Create()
    {
        return new TempDatabaseFixture(TempDirectoryFixture.Create());
    }

    public void Dispose()
    {
        directory.Dispose();
    }
}

internal sealed class TempDirectoryFixture : IDisposable
{
    private TempDirectoryFixture(string path)
    {
        Path = path;
        Directory.CreateDirectory(path);
    }

    public string Path { get; }

    public static TempDirectoryFixture Create()
    {
        return new TempDirectoryFixture(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"FirebirdAdminTests-{Guid.NewGuid():N}"));
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
