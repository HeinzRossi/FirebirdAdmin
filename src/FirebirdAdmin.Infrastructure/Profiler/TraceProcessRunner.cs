using System.Diagnostics;

namespace FirebirdAdmin.Infrastructure.Profiler;

public sealed class TraceProcessRunner : ITraceProcessRunner
{
    public async Task<int> RunAsync(
        TraceProcessRequest request,
        Func<string, CancellationToken, Task> onOutputLine,
        Func<string, CancellationToken, Task> onErrorLine,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in request.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (request.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in request.EnvironmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        process.Start();

        var outputTask = ReadLinesAsync(process.StandardOutput, onOutputLine, cancellationToken);
        var errorTask = ReadLinesAsync(process.StandardError, onErrorLine, cancellationToken);

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
        return process.ExitCode;
    }

    private static async Task ReadLinesAsync(
        TextReader reader,
        Func<string, CancellationToken, Task> onLine,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            await onLine(line, cancellationToken);
        }
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
