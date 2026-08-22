using FirebirdAdmin.Presentation.Wpf.Shell;

namespace FirebirdAdmin.Presentation.Wpf;

public partial class MainWindow
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
