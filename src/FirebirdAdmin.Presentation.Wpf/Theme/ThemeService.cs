using System.IO;
using System.Text.Json;
using System.Windows;

namespace FirebirdAdmin.Presentation.Wpf.Theme;

public sealed class ThemeService : IThemeService
{
    private const string ThemePathPrefix = "pack://application:,,,/FirebirdAdmin.Presentation.Wpf;component/Shared/DesignSystem/Themes/";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string settingsPath;

    public ThemeService()
        : this(GetDefaultSettingsPath())
    {
    }

    public ThemeService(string settingsPath)
    {
        this.settingsPath = settingsPath;
        CurrentTheme = LoadThemeOrDefault();
        ApplyToResources(CurrentTheme);
    }

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;
        ApplyToResources(theme);
        SaveTheme(theme);
    }

    public AppTheme Toggle()
    {
        var next = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
        Apply(next);
        return next;
    }

    private void ApplyToResources(AppTheme theme)
    {
        if (System.Windows.Application.Current is null)
        {
            return;
        }

        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existingTheme = dictionaries.FirstOrDefault(IsThemeDictionary);
        var replacement = new ResourceDictionary
        {
            Source = new Uri($"{ThemePathPrefix}{theme}.xaml", UriKind.Absolute)
        };

        if (existingTheme is null)
        {
            dictionaries.Add(replacement);
            return;
        }

        var index = dictionaries.IndexOf(existingTheme);
        dictionaries[index] = replacement;
    }

    private AppTheme LoadThemeOrDefault()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return AppTheme.Dark;
            }

            var settings = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(settingsPath), JsonOptions);
            return Enum.TryParse<AppTheme>(settings?.Theme, ignoreCase: true, out var theme)
                ? theme
                : AppTheme.Dark;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return AppTheme.Dark;
        }
    }

    private void SaveTheme(AppTheme theme)
    {
        try
        {
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(new ThemeSettings(theme.ToString()), JsonOptions);
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
        }
    }

    private static string GetDefaultSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "FirebirdAdmin", "settings.json");
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        return source is not null &&
            source.Contains("/Shared/DesignSystem/Themes/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ThemeSettings(string? Theme);
}
