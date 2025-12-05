using System.Text.Json;
using SolutionGenerator.Core.Executors;
using SolutionGenerator.Core.Generators;
using SolutionGenerator.Core.Models;
using SolutionGenerator.Core.Modifiers;
using SolutionGenerator.Core.Validators;
using Generators = SolutionGenerator.Core.Generators;

namespace SolutionGenerator.Core;

public class SolutionGeneratorService
{
    private readonly JsonSchemaValidator _validator;
    private readonly Generators.SolutionGenerator _solutionGenerator;
    private readonly PackagePropsGenerator _packagePropsGenerator;
    private readonly CsprojModifier _csprojModifier;
    private readonly FileGenerator _fileGenerator;
    private readonly string _outputPath;
    private readonly DotNetCliExecutor _cliExecutor;

    public SolutionGeneratorService(
        string schemaPath,
        string outputPath)
    {
        _outputPath = outputPath;
        _validator = new JsonSchemaValidator(schemaPath);
        
        _cliExecutor = new DotNetCliExecutor(outputPath);
        _solutionGenerator = new Generators.SolutionGenerator(_cliExecutor, outputPath);
        _packagePropsGenerator = new PackagePropsGenerator();
        _csprojModifier = new CsprojModifier();
        _fileGenerator = new FileGenerator();
    }

    public async Task<GenerationResult> GenerateAsync(string jsonConfigPath, CancellationToken cancellationToken = default)
    {
        // 1. Načtení a validace JSON
        var jsonContent = await File.ReadAllTextAsync(jsonConfigPath, cancellationToken);
        var validationResult = await _validator.ValidateAsync(jsonContent);

        if (!validationResult.IsValid)
        {
            return new GenerationResult
            {
                Success = false,
                Errors = validationResult.Errors
            };
        }

        // 2. Deserializace
        var config = JsonSerializer.Deserialize<SolutionConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config?.Solution == null)
        {
            return new GenerationResult
            {
                Success = false,
                Errors = new List<string> { "Invalid configuration structure" }
            };
        }

        var solution = config.Solution;
        var defaultFramework = solution.TargetFramework ?? "net10.0";

        // 3. Vytvoření výstupního adresáře
        Directory.CreateDirectory(_outputPath);

        // 4. Generování solution
        var solutionPath = await _solutionGenerator.GenerateAsync(solution, cancellationToken);
        
        // Vytvoření ProjectGenerator se správným názvem solution
        var projectGenerator = new ProjectGenerator(_cliExecutor, _outputPath, solution.Name);

        // 5. Generování Directory.packages.props
        _packagePropsGenerator.Generate(_outputPath, solution);

        // 6. Generování projektů (v topologickém pořadí podle závislostí)
        var projects = TopologicalSort(solution.Projects);
        var projectPaths = new Dictionary<string, string>();

        foreach (var project in projects)
        {
            var csprojPath = await projectGenerator.GenerateAsync(project, defaultFramework, cancellationToken);
            projectPaths[project.Name] = csprojPath;

            // Úprava .csproj souboru
            var projectPath = Path.GetDirectoryName(csprojPath)!;
            _csprojModifier.Modify(csprojPath, project, defaultFramework, solution.CentralPackageManagement);

            // Generování souborů
            _fileGenerator.GenerateFiles(projectPath, project);
        }

        return new GenerationResult
        {
            Success = true,
            SolutionPath = solutionPath,
            ProjectPaths = projectPaths
        };
    }

    private List<ProjectDefinition> TopologicalSort(List<ProjectDefinition> projects)
    {
        var sorted = new List<ProjectDefinition>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();
        var projectMap = projects.ToDictionary(p => p.Name);

        foreach (var project in projects)
        {
            if (!visited.Contains(project.Name))
            {
                Visit(project, projectMap, visited, visiting, sorted);
            }
        }

        return sorted;
    }

    private void Visit(
        ProjectDefinition project,
        Dictionary<string, ProjectDefinition> projectMap,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<ProjectDefinition> sorted)
    {
        if (visiting.Contains(project.Name))
        {
            throw new InvalidOperationException($"Circular dependency detected involving project: {project.Name}");
        }

        if (visited.Contains(project.Name))
        {
            return;
        }

        visiting.Add(project.Name);

        foreach (var dependency in project.Dependencies)
        {
            if (projectMap.TryGetValue(dependency, out var depProject))
            {
                Visit(depProject, projectMap, visited, visiting, sorted);
            }
        }

        visiting.Remove(project.Name);
        visited.Add(project.Name);
        sorted.Add(project);
    }
}

public class GenerationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? SolutionPath { get; set; }
    public Dictionary<string, string> ProjectPaths { get; set; } = new();
}

