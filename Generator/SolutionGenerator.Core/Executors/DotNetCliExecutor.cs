using System.Diagnostics;

namespace SolutionGenerator.Core.Executors;

public class DotNetCliExecutor
{
    private readonly string _workingDirectory;

    public DotNetCliExecutor(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public async Task<int> ExecuteAsync(string command, string arguments, CancellationToken cancellationToken = default)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{command} {arguments}",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new Exception($"dotnet {command} failed with exit code {process.ExitCode}: {error}");
        }

        return process.ExitCode;
    }

    public async Task<string> ExecuteWithOutputAsync(string command, string arguments, CancellationToken cancellationToken = default)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{command} {arguments}",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new Exception($"dotnet {command} failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }
}

