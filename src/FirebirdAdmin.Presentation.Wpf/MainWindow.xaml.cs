using FirebirdAdmin.Presentation.Wpf.Shell;

namespace FirebirdAdmin.Presentation.Wpf;

public partial class MainWindow
{
    private readonly ShellViewModel viewModel;

    public MainWindow(ShellViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void SaveProfileButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.SaveProfileAsync(PasswordInput.Password);
    }

    private async void TestConnectionButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.TestConnectionAsync(PasswordInput.Password);
    }

    private async void ConnectButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await viewModel.ConnectAsync(PasswordInput.Password);
    }
}
