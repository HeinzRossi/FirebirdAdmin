using System.Windows;
using System.IO;
using FirebirdAdmin.Application.DependencyInjection;
using FirebirdAdmin.Infrastructure.DependencyInjection;
using FirebirdAdmin.Presentation.Wpf;
using FirebirdAdmin.Presentation.Wpf.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FirebirdAdmin.Bootstrapper;

public partial class App : System.Windows.Application
{
    private IHost? host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var splash = new SplashWindow();
        splash.Show();

        try
        {
            host = Host.CreateDefaultBuilder(e.Args)
                .UseSerilog((context, configuration) =>
                {
                    var logDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "FirebirdAdmin",
                        "Logs");

                    _ = context;

                    configuration
                        .Enrich.FromLogContext()
                        .WriteTo.File(
                            Path.Combine(logDirectory, "firebird-admin-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 14);
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddApplication()
                        .AddInfrastructure()
                        .AddPresentation();
                })
                .Build();

            await host.StartAsync();

            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        finally
        {
            splash.Close();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null)
        {
            await host.StopAsync(TimeSpan.FromSeconds(5));
            host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
