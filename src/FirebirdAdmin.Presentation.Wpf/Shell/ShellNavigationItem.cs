namespace FirebirdAdmin.Presentation.Wpf.Shell;

public sealed record ShellNavigationItem(
    ShellWorkspace Workspace,
    string Title,
    string Shortcut,
    string AccessText);
