using SolutionGenerator.Core.Models;

namespace SolutionGenerator.Core.Generators;

public class FileGenerator
{
    public void GenerateFiles(string projectPath, ProjectDefinition project)
    {
        // Vytvoření složek
        if (project.Folders != null)
        {
            foreach (var folder in project.Folders)
            {
                var folderPath = Path.Combine(projectPath, folder);
                Directory.CreateDirectory(folderPath);
            }
        }

        // Generování souborů
        foreach (var file in project.Files)
        {
            var filePath = Path.Combine(projectPath, file.Path);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string content;
            if (!string.IsNullOrEmpty(file.Content))
            {
                content = file.Content;
            }
            else
            {
                content = GenerateFromTemplate(file.Template ?? "custom", file.Namespace ?? project.Name, project.Name);
            }

            File.WriteAllText(filePath, content);
        }
    }

    private string GenerateFromTemplate(string template, string namespaceName, string projectName)
    {
        return template.ToLower() switch
        {
            "console-program" => GenerateConsoleProgram(namespaceName),
            "web-program" => GenerateWebProgram(namespaceName),
            "blazor-program" => GenerateBlazorProgram(namespaceName),
            "win-program" => GenerateWinProgram(namespaceName),
            "module-class" => GenerateModuleClass(namespaceName),
            "controller" => GenerateController(namespaceName),
            "business-object" => GenerateBusinessObject(namespaceName),
            _ => GenerateCustomClass(namespaceName)
        };
    }

    private string GenerateConsoleProgram(string namespaceName)
    {
        return $@"// See https://aka.ms/new-console-template for more information
using System;

namespace {namespaceName};

class Program
{{
    static void Main(string[] args)
    {{
        Console.WriteLine(""Hello, World!"");
    }}
}}";
    }

    private string GenerateWebProgram(string namespaceName)
    {
        return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace {namespaceName};

class Program
{{
    static void Main(string[] args)
    {{
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }}
}}";
    }

    private string GenerateBlazorProgram(string namespaceName)
    {
        return $@"using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace {namespaceName};

class Program
{{
    static void Main(string[] args)
    {{
        // Blazor server setup
    }}
}}";
    }

    private string GenerateWinProgram(string namespaceName)
    {
        return $@"using System;
using System.Windows.Forms;

namespace {namespaceName};

static class Program
{{
    [STAThread]
    static void Main()
    {{
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }}
}}";
    }

    private string GenerateModuleClass(string namespaceName)
    {
        return $@"namespace {namespaceName};

public class Module
{{
    public Module()
    {{
    }}
}}";
    }

    private string GenerateController(string namespaceName)
    {
        return $@"using Microsoft.AspNetCore.Mvc;

namespace {namespaceName};

[ApiController]
[Route(""api/[controller]"")]
public class Controller : ControllerBase
{{
    [HttpGet]
    public IActionResult Get()
    {{
        return Ok();
    }}
}}";
    }

    private string GenerateBusinessObject(string namespaceName)
    {
        return $@"namespace {namespaceName};

public class BusinessObject
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }} = string.Empty;
}}";
    }

    private string GenerateCustomClass(string namespaceName)
    {
        return $@"namespace {namespaceName};

public class Class1
{{
}}";
    }
}

