namespace FirebirdAdmin.Application.History;

public interface IRetentionPolicyService
{
    Task<RetentionPolicy> GetPolicyAsync(CancellationToken cancellationToken);
    Task ApplyRetentionAsync(CancellationToken cancellationToken);
}
