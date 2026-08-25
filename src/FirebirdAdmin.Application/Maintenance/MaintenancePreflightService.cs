using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Maintenance;

public sealed class MaintenancePreflightService : IMaintenancePreflightService
{
    public Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var review = new List<string>
        {
            $"Operação: {FormatOperationType(request.Type)}",
            $"Origem: {request.Source}"
        };

        if (!string.IsNullOrWhiteSpace(request.Target))
        {
            review.Add($"Destino: {request.Target}");
        }

        var requiredTool = request.Type is MaintenanceOperationType.Backup or MaintenanceOperationType.Restore
            ? FirebirdToolKind.Backup
            : FirebirdToolKind.Fix;

        var tool = request.Connection.Toolset.Candidates.FirstOrDefault(candidate => candidate.Kind == requiredTool && candidate.IsAvailable);
        if (tool is null)
        {
            errors.Add(requiredTool is FirebirdToolKind.Backup ? "Ferramenta gbak não encontrada." : "Ferramenta gfix não encontrada.");
        }
        else
        {
            review.Add($"Toolset: {tool.Path}");
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            errors.Add("Caminho de origem obrigatório.");
        }

        if (request.Type is MaintenanceOperationType.Backup && string.IsNullOrWhiteSpace(request.Target))
        {
            errors.Add("Caminho do backup obrigatório.");
        }

        if (request.Type is MaintenanceOperationType.Restore)
        {
            if (string.IsNullOrWhiteSpace(request.Target))
            {
                errors.Add("Caminho do banco restaurado obrigatório.");
            }
            else if (File.Exists(request.Target))
            {
                errors.Add("Restore não sobrescreve banco existente. Informe um caminho de novo banco no campo Destino.");
            }
        }

        ValidateTargetDirectory(request, errors, warnings);

        if (!request.Confirmed)
        {
            warnings.Add("Operação ainda não confirmada.");
        }

        return Task.FromResult(new MaintenancePreflightResult(errors.Count == 0, errors, warnings, review));
    }

    private static void ValidateTargetDirectory(MaintenanceRequest request, List<string> errors, List<string> warnings)
    {
        var path = request.Target ?? request.Source;
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            warnings.Add("Diretório não informado explicitamente; a ferramenta Firebird resolverá o caminho.");
            return;
        }

        if (request.Type is MaintenanceOperationType.Backup or MaintenanceOperationType.Restore && !Directory.Exists(directory))
        {
            errors.Add($"Diretório não existe: {directory}");
            return;
        }

        if (Directory.Exists(directory))
        {
            try
            {
                var probe = Path.Combine(directory, $".firebird-admin-probe-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
            }
            catch (Exception ex)
            {
                errors.Add($"Sem permissão de escrita em {directory}: {ex.Message}");
            }
        }
    }

    private static string FormatOperationType(MaintenanceOperationType type)
    {
        return type switch
        {
            MaintenanceOperationType.Backup => "Backup",
            MaintenanceOperationType.Restore => "Restore",
            MaintenanceOperationType.Validation => "Validação",
            MaintenanceOperationType.Sweep => "Sweep",
            _ => type.ToString()
        };
    }
}
