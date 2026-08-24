using System.Collections.ObjectModel;
using FirebirdAdmin.Application.Diagnostics;

namespace FirebirdAdmin.Infrastructure.Diagnostics;

public sealed class InAppNotificationChannel : INotificationChannel
{
    public ObservableCollection<Alert> Notifications { get; } = [];

    public Task NotifyAsync(Alert alert, CancellationToken cancellationToken)
    {
        Notifications.Add(alert);
        return Task.CompletedTask;
    }
}
