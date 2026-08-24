namespace FirebirdAdmin.Application.Diagnostics;

public interface INotificationChannel
{
    Task NotifyAsync(Alert alert, CancellationToken cancellationToken);
}
