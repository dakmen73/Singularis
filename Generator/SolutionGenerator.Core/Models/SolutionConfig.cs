using System.Text.Json.Serialization;

namespace SolutionGenerator.Core.Models;

public class SolutionConfig
{
    [JsonPropertyName("solution")]
    public SolutionDefinition Solution { get; set; } = null!;
}

public class SolutionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    [JsonPropertyName("configurations")]
    public List<string> Configurations { get; set; } = new() { "Debug", "Release" };

    [JsonPropertyName("centralPackageManagement")]
    public bool CentralPackageManagement { get; set; } = true;

    [JsonPropertyName("centralPackageFloatingVersions")]
    public bool CentralPackageFloatingVersions { get; set; } = true;

    [JsonPropertyName("packages")]
    public List<PackageDefinition> Packages { get; set; } = new();

    [JsonPropertyName("projects")]
    public List<ProjectDefinition> Projects { get; set; } = new();

    [JsonPropertyName("folders")]
    public List<string>? Folders { get; set; }
}

public class ProjectDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    [JsonPropertyName("outputType")]
    public string? OutputType { get; set; }

    [JsonPropertyName("useWindowsForms")]
    public bool? UseWindowsForms { get; set; }

    [JsonPropertyName("checkEolTargetFramework")]
    public bool? CheckEolTargetFramework { get; set; }

    [JsonPropertyName("deterministic")]
    public bool? Deterministic { get; set; }

    [JsonPropertyName("assemblyVersion")]
    public string? AssemblyVersion { get; set; }

    [JsonPropertyName("fileVersion")]
    public string? FileVersion { get; set; }

    [JsonPropertyName("implicitUsings")]
    public bool? ImplicitUsings { get; set; }

    [JsonPropertyName("isPackable")]
    public bool? IsPackable { get; set; }

    [JsonPropertyName("applicationIcon")]
    public string? ApplicationIcon { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    [JsonPropertyName("nugetPackages")]
    public List<string> NuGetPackages { get; set; } = new();

    [JsonPropertyName("usingAliases")]
    public List<UsingAlias>? UsingAliases { get; set; }

    [JsonPropertyName("files")]
    public List<ProjectFile> Files { get; set; } = new();

    [JsonPropertyName("embeddedResources")]
    public List<string> EmbeddedResources { get; set; } = new();

    [JsonPropertyName("contentFiles")]
    public List<ContentFile> ContentFiles { get; set; } = new();

    [JsonPropertyName("folders")]
    public List<string>? Folders { get; set; }
}

public class PackageDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

public class ProjectFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }
}

public class ContentFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("copyToOutput")]
    public string CopyToOutput { get; set; } = "Never";
}

public class UsingAlias
{
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;
}

