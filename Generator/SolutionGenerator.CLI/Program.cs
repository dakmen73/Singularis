using System.CommandLine;
using SolutionGenerator.Core;
using SolutionGenerator.Core.Generators;
using SolutionGenerator.Core.Tests;

namespace SolutionGenerator.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Test external reference
        if (args.Length > 0 && args[0] == "--test-external-ref")
        {
            await ExternalRefTest.TestExternalReference();
            return 0;
        }

        // Generate XPO code from JSON
        if (args.Length > 0 && args[0] == "generate-xpo")
        {
            var xpoJsonOption = new Option<FileInfo>(
                aliases: new[] { "--json", "-j" },
                description: "Path to JSON file with XPO data model")
            {
                IsRequired = true
            };

            var xpoOutputOption = new Option<DirectoryInfo>(
                aliases: new[] { "--output", "-o" },
                description: "Output directory for generated C# files",
                getDefaultValue: () => new DirectoryInfo("./GeneratedCode"))
            {
                IsRequired = false
            };

            var xpoTemplatesOption = new Option<DirectoryInfo>(
                aliases: new[] { "--templates", "-t" },
                description: "Directory with Scriban templates",
                getDefaultValue: () => new DirectoryInfo("./Templates"))
            {
                IsRequired = false
            };

            var generateCommand = new Command("generate-xpo", "Generate C# code from XPO JSON using Scriban templates")
            {
                xpoJsonOption,
                xpoOutputOption,
                xpoTemplatesOption
            };

            generateCommand.SetHandler(async (json, output, templates) =>
            {
                await ExecuteGenerateXpoAsync(json, output, templates);
            }, xpoJsonOption, xpoOutputOption, xpoTemplatesOption);

            return await generateCommand.InvokeAsync(args.Skip(1).ToArray());
        }

        var configOption = new Option<FileInfo>(
            aliases: new[] { "--config", "-c" },
            description: "Path to JSON configuration file")
        {
            IsRequired = true
        };

        var outputOption = new Option<DirectoryInfo>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for generated solution",
            getDefaultValue: () => new DirectoryInfo("./GeneratedSolution"))
        {
            IsRequired = false
        };

        var schemaOption = new Option<FileInfo>(
            aliases: new[] { "--schema", "-s" },
            description: "Path to JSON schema file",
            getDefaultValue: () => new FileInfo("./solution.schema.json"))
        {
            IsRequired = false
        };

        var rootCommand = new RootCommand("Generates .NET solution structure from JSON configuration")
        {
            configOption,
            outputOption,
            schemaOption
        };

        rootCommand.SetHandler(async (config, output, schema) =>
        {
            await ExecuteGenerateAsync(config, output, schema);
        }, configOption, outputOption, schemaOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ExecuteGenerateAsync(FileInfo config, DirectoryInfo output, FileInfo schema)
    {
        try
        {
            Console.WriteLine($"Loading configuration from: {config.FullName}");
            
            if (!config.Exists)
            {
                Console.WriteLine($"Error: Configuration file not found: {config.FullName}");
                return;
            }

            if (!schema.Exists)
            {
                Console.WriteLine($"Warning: Schema file not found: {schema.FullName}");
                Console.WriteLine("Continuing without schema validation...");
            }

            var schemaPath = schema.Exists ? schema.FullName : string.Empty;
            var service = new SolutionGeneratorService(schemaPath, output.FullName);

            Console.WriteLine($"Generating solution to: {output.FullName}");
            Console.WriteLine();

            var result = await service.GenerateAsync(config.FullName);

            if (result.Success)
            {
                Console.WriteLine("✓ Solution generated successfully!");
                Console.WriteLine($"  Solution: {result.SolutionPath}");
                Console.WriteLine($"  Projects: {result.ProjectPaths.Count}");
                foreach (var project in result.ProjectPaths)
                {
                    Console.WriteLine($"    - {project.Key}: {project.Value}");
                }
            }
            else
            {
                Console.WriteLine("✗ Generation failed!");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
            }
        }
    }

    private static async Task ExecuteGenerateXpoAsync(FileInfo json, DirectoryInfo output, DirectoryInfo templates)
    {
        try
        {
            Console.WriteLine($"Loading XPO data from: {json.FullName}");
            
            if (!json.Exists)
            {
                Console.WriteLine($"Error: JSON file not found: {json.FullName}");
                return;
            }

            if (!templates.Exists)
            {
                Console.WriteLine($"Error: Templates directory not found: {templates.FullName}");
                return;
            }

            Console.WriteLine($"Using templates from: {templates.FullName}");
            Console.WriteLine($"Generating code to: {output.FullName}");
            Console.WriteLine();

            var generator = new XpoCodeGenerator(templates.FullName, output.FullName);
            var result = await generator.GenerateFromJsonAsync(json.FullName);

            if (result.Success)
            {
                Console.WriteLine($"✓ Code generation completed successfully!");
                Console.WriteLine($"  Generated {result.SuccessCount} classes");
                Console.WriteLine($"  Total files: {result.GeneratedFiles.Count}");
                foreach (var file in result.GeneratedFiles)
                {
                    Console.WriteLine($"    - {file}");
                }
            }
            else
            {
                Console.WriteLine("✗ Code generation failed!");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Error: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
            }
        }
    }
}

