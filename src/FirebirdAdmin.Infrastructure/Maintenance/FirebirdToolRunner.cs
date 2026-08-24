using System.Diagnostics;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Infrastructure.Security;

namespace FirebirdAdmin.Infrastructure.Maintenance;

public sealed class FirebirdToolRunner : IFirebirdToolRunner
{
    public async Task<ToolExecutionResult> ExecuteAsync(
        Guid operationId,
        FirebirdToolCommand command,
        IProgress<MaintenanceLogLine> progress,
        CancellationToken cancellationToken)
    {
        var logs = new List<MaintenanceLogLine>();
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command.ExecutablePath,
            WorkingDirectory = command.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in command.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        foreach (var (key, value) in command.EnvironmentVariables)
        {
            process.StartInfo.Environment[key] = value;
        }

        Report(operationId, "info", $"Executando {Path.GetFileName(command.ExecutablePath)} {SecretMasker.MaskSecrets(string.Join(' ', command.Arguments))}", progress, logs);
        process.Start();

        var outputTask = ReadLinesAsync(operationId, "stdout", process.StandardOutput, progress, logs, cancellationToken);
        var errorTask = ReadLinesAsync(operationId, "stderr", process.StandardError, progress, logs, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        await Task.WhenAll(outputTask, errorTask);
        return new ToolExecutionResult(process.ExitCode, logs);
    }

    private static async Task ReadLinesAsync(
        Guid operationId,
        string stream,
        TextReader reader,
        IProgress<MaintenanceLogLine> progress,
        List<MaintenanceLogLine> logs,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            Report(operationId, stream, line, progress, logs);
        }
    }

    private static void Report(Guid operationId, string stream, string text, IProgress<MaintenanceLogLine> progress, List<MaintenanceLogLine> logs)
    {
        var line = new MaintenanceLogLine(operationId, DateTimeOffset.UtcNow, stream, SecretMasker.MaskSecrets(text));
        logs.Add(line);
        progress.Report(line);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
