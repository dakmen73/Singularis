using SolutionGenerator.Core.Executors;
using SolutionGenerator.Core.Models;

namespace SolutionGenerator.Core.Generators;

public class ProjectGenerator
{
    private readonly DotNetCliExecutor _cliExecutor;
    private readonly string _outputPath;
    private readonly string _solutionName;

    public ProjectGenerator(DotNetCliExecutor cliExecutor, string outputPath, string solutionName)
    {
        _cliExecutor = cliExecutor;
        _outputPath = outputPath;
        _solutionName = solutionName;
    }

    public async Task<string> GenerateAsync(ProjectDefinition project, string defaultFramework, CancellationToken cancellationToken = default)
    {
        var requestedFramework = project.TargetFramework ?? defaultFramework;
        var template = GetDotNetTemplate(project.Type);
        var projectPath = Path.Combine(_outputPath, project.Folder ?? project.Name);
        var projectName = project.Name;

        // Získání nejnovější podporované verze frameworku pro danou šablonu
        var framework = await GetSupportedFrameworkAsync(template, requestedFramework, cancellationToken);

        // Vytvoření projektu pomocí dotnet CLI
        await _cliExecutor.ExecuteAsync(
            "new",
            $"{template} -n {projectName} -f {framework} -o \"{projectPath}\"",
            cancellationToken);

        var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");

        // Přidání projektu do solution
        var solutionPath = Path.Combine(_outputPath, $"{_solutionName}.slnx");
        if (!File.Exists(solutionPath))
        {
            solutionPath = Path.Combine(_outputPath, $"{_solutionName}.sln");
        }

        await _cliExecutor.ExecuteAsync(
            "sln",
            $"\"{solutionPath}\" add \"{csprojPath}\"",
            cancellationToken);

        return csprojPath;
    }

    private string GetDotNetTemplate(string projectType)
    {
        return projectType.ToLower() switch
        {
            "console" => "console",
            "library" => "classlib",
            "web" => "webapi",
            "blazor" => "blazorserver",
            "win" => "winforms",
            "test" => "xunit",
            _ => throw new ArgumentException($"Unknown project type: {projectType}")
        };
    }

    private async Task<string> GetSupportedFrameworkAsync(string template, string requestedFramework, CancellationToken cancellationToken)
    {
        // Získání seznamu podporovaných frameworků pro šablonu
        try
        {
            var helpOutput = await _cliExecutor.ExecuteWithOutputAsync(
                "new",
                $"{template} -h",
                cancellationToken);

            // Parsování výstupu pro nalezení podporovaných frameworků
            // Prioritizujeme: net10.0 > net9.0 > net8.0 > net6.0
            var supportedFrameworks = new[] { "net10.0", "net9.0", "net8.0", "net6.0" };
            
            // Zkontrolujeme, zda je požadovaný framework podporován
            var requestedNormalized = requestedFramework.Replace("-windows", "").Replace("-", "");
            foreach (var framework in supportedFrameworks)
            {
                if (helpOutput.Contains(framework))
                {
                    var frameworkNormalized = framework.Replace(".0", "");
                    // Pokud je požadovaný framework podporován, použijeme ho
                    if (requestedNormalized.Contains(frameworkNormalized))
                    {
                        return framework;
                    }
                }
            }

            // Pokud požadovaný framework není podporován, použijeme nejnovější dostupný
            foreach (var framework in supportedFrameworks)
            {
                if (helpOutput.Contains(framework))
                {
                    return framework;
                }
            }
        }
        catch
        {
            // Pokud se nepodaří zjistit, použijeme požadovaný framework
        }

        // Fallback: pokud šablona nepodporuje požadovaný framework, použijeme nejnovější dostupný
        // Pro některé šablony (např. blazorserver) může být pouze net6.0
        return requestedFramework;
    }
}

