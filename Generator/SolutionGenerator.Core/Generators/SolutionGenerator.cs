using SolutionGenerator.Core.Executors;
using SolutionGenerator.Core.Models;

namespace SolutionGenerator.Core.Generators;

public class SolutionGenerator
{
    private readonly DotNetCliExecutor _cliExecutor;
    private readonly string _outputPath;

    public SolutionGenerator(DotNetCliExecutor cliExecutor, string outputPath)
    {
        _cliExecutor = cliExecutor;
        _outputPath = outputPath;
    }

    public async Task<string> GenerateAsync(SolutionDefinition solution, CancellationToken cancellationToken = default)
    {
        // Vytvoření solution pomocí dotnet CLI
        var solutionName = solution.Name;
        var slnPath = Path.Combine(_outputPath, $"{solutionName}.sln");

        await _cliExecutor.ExecuteAsync(
            "new",
            $"sln -n {solutionName} -o \"{_outputPath}\"",
            cancellationToken);

        // Migrace na .slnx (pokud je podporováno)
        try
        {
            await _cliExecutor.ExecuteAsync(
                "sln",
                $"\"{slnPath}\" migrate",
                cancellationToken);
        }
        catch
        {
            // Pokud migrace není podporována, pokračujeme s .sln
            // VS 2026 možná automaticky vytváří .slnx
        }

        // Kontrola, zda existuje .slnx soubor
        var slnxPath = Path.Combine(_outputPath, $"{solutionName}.slnx");
        if (File.Exists(slnxPath))
        {
            return slnxPath;
        }

        // Pokud ne, vytvoříme .slnx ručně z .sln
        if (File.Exists(slnPath))
        {
            await ConvertSlnToSlnxAsync(slnPath, solution, cancellationToken);
            return slnxPath;
        }

        return slnPath;
    }

    private async Task ConvertSlnToSlnxAsync(string slnPath, SolutionDefinition solution, CancellationToken cancellationToken)
    {
        var slnxPath = Path.Combine(Path.GetDirectoryName(slnPath)!, $"{solution.Name}.slnx");

        // Načtení projektů z .sln (jednoduchá implementace)
        var projects = solution.Projects;

        // Vytvoření .slnx XML
        using var writer = new System.IO.StringWriter();
        writer.WriteLine("<Solution>");
        writer.WriteLine("  <Configurations>");

        foreach (var config in solution.Configurations)
        {
            writer.WriteLine($"    <BuildType Name=\"{config}\" />");
        }

        writer.WriteLine("  </Configurations>");

        foreach (var project in projects)
        {
            var projectPath = Path.Combine(project.Folder ?? project.Name, $"{project.Name}.csproj");
            writer.WriteLine($"  <Project Path=\"{projectPath}\" />");
        }

        writer.WriteLine("</Solution>");

        await File.WriteAllTextAsync(slnxPath, writer.ToString(), cancellationToken);
    }
}

