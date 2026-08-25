namespace FirebirdAdmin.Presentation.Wpf.Theme;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void Apply(AppTheme theme);

    AppTheme Toggle();
}
