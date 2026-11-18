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
        return request.UseFileRedirection
            ? await RunWithFileRedirectionAsync(request, onOutputLine, onErrorLine, cancellationToken)
            : await RunWithPipeRedirectionAsync(request, onOutputLine, onErrorLine, cancellationToken);
    }

    private static async Task<int> RunWithPipeRedirectionAsync(
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

        var outputTask = ReadLinesAsync(process.StandardOutput, onOutputLine);
        var errorTask = ReadLinesAsync(process.StandardError, onErrorLine);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await WaitForProcessExitAsync(process);
            await DrainProcessOutputAsync(outputTask, errorTask);
            throw;
        }

        await Task.WhenAll(outputTask, errorTask);
        return process.ExitCode;
    }

    private static async Task<int> RunWithFileRedirectionAsync(
        TraceProcessRequest request,
        Func<string, CancellationToken, Task> onOutputLine,
        Func<string, CancellationToken, Task> onErrorLine,
        CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"firebird-admin-trace-out-{Guid.NewGuid():N}.log");
        var errorPath = Path.Combine(Path.GetTempPath(), $"firebird-admin-trace-err-{Guid.NewGuid():N}.log");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            Arguments = $"/d /s /c \"{BuildRedirectedCommand(request, outputPath, errorPath)}\"",
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (request.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in request.EnvironmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        long outputPosition = 0;
        long errorPosition = 0;

        try
        {
            process.Start();

            while (!process.HasExited)
            {
                outputPosition = await ReadAvailableLinesAsync(outputPath, outputPosition, onOutputLine);
                errorPosition = await ReadAvailableLinesAsync(errorPath, errorPosition, onErrorLine);
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }

            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await WaitForProcessExitAsync(process);
            throw;
        }
        finally
        {
            await ReadAvailableLinesAsync(outputPath, outputPosition, onOutputLine);
            await ReadAvailableLinesAsync(errorPath, errorPosition, onErrorLine);
            DeleteIfExists(outputPath);
            DeleteIfExists(errorPath);
        }

        return process.ExitCode;
    }

    private static async Task ReadLinesAsync(
        TextReader reader,
        Func<string, CancellationToken, Task> onLine)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            await onLine(line, CancellationToken.None);
        }
    }

    private static async Task WaitForProcessExitAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static async Task DrainProcessOutputAsync(params Task[] readerTasks)
    {
        try
        {
            await Task.WhenAll(readerTasks).WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task<long> ReadAvailableLinesAsync(
        string path,
        long position,
        Func<string, CancellationToken, Task> onLine)
    {
        if (!File.Exists(path))
        {
            return position;
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        if (position > stream.Length)
        {
            position = 0;
        }

        stream.Seek(position, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            await onLine(line, CancellationToken.None);
        }

        return stream.Position;
    }

    private static string BuildRedirectedCommand(TraceProcessRequest request, string outputPath, string errorPath)
    {
        var parts = new List<string> { QuoteForCmd(request.ExecutablePath) };
        parts.AddRange(request.Arguments.Select(QuoteForCmd));
        parts.Add("1>");
        parts.Add(QuoteForCmd(outputPath));
        parts.Add("2>");
        parts.Add(QuoteForCmd(errorPath));
        return string.Join(" ", parts);
    }

    private static string QuoteForCmd(string value)
    {
        if (value.Contains('"'))
        {
            throw new InvalidOperationException("Argumento de processo contém aspas e não pode ser redirecionado com segurança.");
        }

        return "\"" + value.Replace("%", "%%", StringComparison.Ordinal) + "\"";
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

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
