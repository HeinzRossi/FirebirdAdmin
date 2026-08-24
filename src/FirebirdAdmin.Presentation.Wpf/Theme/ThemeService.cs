using System.Windows;

namespace FirebirdAdmin.Presentation.Wpf.Theme;

public sealed class ThemeService : IThemeService
{
    private const string ThemePathPrefix = "pack://application:,,,/FirebirdAdmin.Presentation.Wpf;component/Shared/DesignSystem/Themes/";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;

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

    public AppTheme Toggle()
    {
        var next = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
        Apply(next);
        return next;
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        return source is not null &&
            source.Contains("/Shared/DesignSystem/Themes/", StringComparison.OrdinalIgnoreCase);
    }
}
