using System.Xml.Linq;
using SolutionGenerator.Core.Models;

namespace SolutionGenerator.Core.Modifiers;

public class CsprojModifier
{
    public void Modify(string csprojPath, ProjectDefinition project, string defaultFramework, bool centralPackageManagement)
    {
        var doc = XDocument.Load(csprojPath);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid .csproj file");

        // Najít nebo vytvořit PropertyGroup
        var propertyGroup = root.Elements("PropertyGroup").FirstOrDefault();
        if (propertyGroup == null)
        {
            propertyGroup = new XElement("PropertyGroup");
            root.AddFirst(propertyGroup);
        }

        // Nastavit target framework
        var framework = project.TargetFramework ?? defaultFramework;
        SetOrUpdateProperty(propertyGroup, "TargetFramework", framework);

        // Nastavit další vlastnosti
        if (project.OutputType != null)
        {
            SetOrUpdateProperty(propertyGroup, "OutputType", project.OutputType);
        }

        if (project.UseWindowsForms.HasValue)
        {
            SetOrUpdateProperty(propertyGroup, "UseWindowsForms", project.UseWindowsForms.Value.ToString().ToLower());
        }

        if (project.CheckEolTargetFramework.HasValue)
        {
            SetOrUpdateProperty(propertyGroup, "CheckEolTargetFramework", project.CheckEolTargetFramework.Value.ToString().ToLower());
        }

        if (project.Deterministic.HasValue)
        {
            SetOrUpdateProperty(propertyGroup, "Deterministic", project.Deterministic.Value.ToString().ToLower());
        }

        if (project.AssemblyVersion != null)
        {
            SetOrUpdateProperty(propertyGroup, "AssemblyVersion", project.AssemblyVersion);
        }

        if (project.FileVersion != null)
        {
            SetOrUpdateProperty(propertyGroup, "FileVersion", project.FileVersion);
        }

        if (project.ImplicitUsings.HasValue)
        {
            SetOrUpdateProperty(propertyGroup, "ImplicitUsings", project.ImplicitUsings.Value ? "enable" : "disable");
        }

        if (project.IsPackable.HasValue)
        {
            SetOrUpdateProperty(propertyGroup, "IsPackable", project.IsPackable.Value.ToString().ToLower());
        }

        if (project.ApplicationIcon != null)
        {
            SetOrUpdateProperty(propertyGroup, "ApplicationIcon", project.ApplicationIcon);
        }

        // Přidat Configurations pokud jsou definovány
        // (to je obvykle v solution, ale můžeme to přidat i sem)

        // Přidat Using direktivy s aliasy
        if (project.UsingAliases != null && project.UsingAliases.Any())
        {
            var itemGroup = GetOrCreateItemGroup(root, "Using");
            foreach (var alias in project.UsingAliases)
            {
                var usingElement = new XElement("Using",
                    new XAttribute("Include", alias.Namespace),
                    new XAttribute("Alias", alias.Alias));
                itemGroup.Add(usingElement);
            }
        }

        // Přidat NuGet balíčky (bez verzí, pokud je centralizovaná správa)
        if (project.NuGetPackages.Any())
        {
            var packageItemGroup = GetOrCreateItemGroup(root, "PackageReference");
            foreach (var packageId in project.NuGetPackages)
            {
                var packageRef = new XElement("PackageReference",
                    new XAttribute("Include", packageId));
                
                if (!centralPackageManagement)
                {
                    // Pokud není centralizovaná správa, museli bychom přidat verzi
                    // Ale v našem případě vždy používáme centralizovanou správu
                }

                packageItemGroup.Add(packageRef);
            }
        }

        // Přidat ProjectReference
        if (project.Dependencies.Any())
        {
            var projectRefItemGroup = GetOrCreateItemGroup(root, "ProjectReference");
            foreach (var dependency in project.Dependencies)
            {
                var depPath = $"..\\{dependency}\\{dependency}.csproj";
                var projectRef = new XElement("ProjectReference",
                    new XAttribute("Include", depPath));
                projectRefItemGroup.Add(projectRef);
            }
        }

        // Přidat EmbeddedResource
        if (project.EmbeddedResources.Any())
        {
            var embeddedItemGroup = GetOrCreateItemGroup(root, "EmbeddedResource");
            foreach (var resource in project.EmbeddedResources)
            {
                var embeddedResource = new XElement("EmbeddedResource",
                    new XAttribute("Include", resource));
                embeddedItemGroup.Add(embeddedResource);
            }
        }

        // Přidat Content soubory
        if (project.ContentFiles.Any())
        {
            var contentItemGroup = GetOrCreateItemGroup(root, "Content");
            foreach (var contentFile in project.ContentFiles)
            {
                var content = new XElement("Content",
                    new XAttribute("Include", contentFile.Path));
                
                if (contentFile.CopyToOutput != "Never")
                {
                    var copyToOutput = new XElement("CopyToOutputDirectory", contentFile.CopyToOutput);
                    content.Add(copyToOutput);
                }

                contentItemGroup.Add(content);
            }
        }

        doc.Save(csprojPath);
    }

    private void SetOrUpdateProperty(XElement propertyGroup, string name, string value)
    {
        var existing = propertyGroup.Elements(name).FirstOrDefault();
        if (existing != null)
        {
            existing.Value = value;
        }
        else
        {
            propertyGroup.Add(new XElement(name, value));
        }
    }

    private XElement GetOrCreateItemGroup(XElement root, string itemType)
    {
        // Najít existující ItemGroup s daným typem itemu
        var itemGroup = root.Elements("ItemGroup")
            .FirstOrDefault(g => g.Elements(itemType).Any());

        if (itemGroup == null)
        {
            itemGroup = new XElement("ItemGroup");
            root.Add(itemGroup);
        }

        return itemGroup;
    }
}

