namespace FirebirdAdmin.Presentation.Wpf.Shell;

public sealed record ShellNavigationItem(
    ShellWorkspace Workspace,
    string Title,
    string Shortcut,
    string IconGlyph,
    string AccessText);
