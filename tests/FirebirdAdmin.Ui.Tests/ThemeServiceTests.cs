using System.IO;
using FirebirdAdmin.Presentation.Wpf.Theme;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class ThemeServiceTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "FirebirdAdmin.ThemeTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Constructor_ShouldDefaultToDark_WhenSettingsAreMissing()
    {
        var service = new ThemeService(SettingsPath);

        service.CurrentTheme.Should().Be(AppTheme.Dark);
    }

    [Fact]
    public void Toggle_ShouldPersistSelectedTheme()
    {
        var service = new ThemeService(SettingsPath);

        service.Toggle();

        var restored = new ThemeService(SettingsPath);
        restored.CurrentTheme.Should().Be(AppTheme.Light);
    }

    [Fact]
    public void Constructor_ShouldRestoreSavedTheme()
    {
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(SettingsPath, """{ "theme": "Light" }""");

        var service = new ThemeService(SettingsPath);

        service.CurrentTheme.Should().Be(AppTheme.Light);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private string SettingsPath => Path.Combine(tempDirectory, "settings.json");
}
