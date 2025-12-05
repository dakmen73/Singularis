using System.Xml.Linq;
using SolutionGenerator.Core.Models;

namespace SolutionGenerator.Core.Generators;

public class PackagePropsGenerator
{
    public void Generate(string outputPath, SolutionDefinition solution)
    {
        if (!solution.CentralPackageManagement)
        {
            return;
        }

        var propsPath = Path.Combine(outputPath, "Directory.packages.props");

        var root = new XElement("Project");
        var doc = new XDocument(root);

        // PropertyGroup
        var propertyGroup = new XElement("PropertyGroup");
        propertyGroup.Add(new XElement("ManagePackageVersionsCentrally", "true"));
        
        if (solution.CentralPackageFloatingVersions)
        {
            propertyGroup.Add(new XElement("CentralPackageFloatingVersionsEnabled", "true"));
        }

        root.Add(propertyGroup);

        // ItemGroup s balíčky
        if (solution.Packages.Any())
        {
            var itemGroup = new XElement("ItemGroup");
            
            foreach (var package in solution.Packages)
            {
                var packageVersion = new XElement("PackageVersion",
                    new XAttribute("Include", package.Id),
                    new XAttribute("Version", package.Version));
                itemGroup.Add(packageVersion);
            }

            root.Add(itemGroup);
        }

        doc.Save(propsPath);
    }
}

