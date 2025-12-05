using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XpoExtractor.Models;
using XpoExtractor.Parsers;

namespace XpoExtractor.Extractors;

public class XpoExtractor
{
    private readonly CSharpParser _parser = new();

    public List<ClassInfo> ExtractFromDirectory(string directoryPath)
    {
        var classes = new Dictionary<string, ClassInfo>();
        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.TopDirectoryOnly);

        // Group files by class name
        var fileGroups = files
            .Where(f => !f.EndsWith(".objCompare.cs"))
            .GroupBy(f => GetClassNameFromPath(f))
            .Where(g => g.Key != null);

        foreach (var group in fileGroups)
        {
            var className = group.Key!;
            var classInfo = new ClassInfo { Name = className };

            // Process .generatorL1.cs first (base class, persistent, etc.)
            var l1File = group.FirstOrDefault(f => f.EndsWith(".generatorL1.cs"));
            if (l1File != null)
            {
                var l1Info = _parser.ParseClass(l1File);
                MergeClassInfo(classInfo, l1Info, isL1: true);
            }

            // Process .generatorL2.cs (collections, properties)
            var l2File = group.FirstOrDefault(f => f.EndsWith(".generatorL2.cs"));
            if (l2File != null)
            {
                var l2Info = _parser.ParseClass(l2File);
                MergeClassInfo(classInfo, l2Info, isL2: true);
            }

            // Process .cs (custom code, interfaces, methods)
            var csFile = group.FirstOrDefault(f => 
                !f.EndsWith(".generatorL1.cs") && 
                !f.EndsWith(".generatorL2.cs") &&
                !f.EndsWith(".objCompare.cs"));
            if (csFile != null)
            {
                var csInfo = _parser.ParseClass(csFile);
                MergeClassInfo(classInfo, csInfo, isCustom: true);
            }

            classes[className] = classInfo;
        }

        return classes.Values.ToList();
    }

    private void MergeClassInfo(ClassInfo target, ClassInfo source, 
        bool isL1 = false, bool isL2 = false, bool isCustom = false)
    {
        if (isL1)
        {
            // L1 contains: namespace, base class, persistent, imageName
            if (string.IsNullOrEmpty(target.Namespace))
                target.Namespace = source.Namespace;
            if (string.IsNullOrEmpty(target.BaseClass) && !string.IsNullOrEmpty(source.BaseClass))
                target.BaseClass = source.BaseClass;
            if (string.IsNullOrEmpty(target.Persistent) && !string.IsNullOrEmpty(source.Persistent))
                target.Persistent = source.Persistent;
            if (string.IsNullOrEmpty(target.ImageName) && !string.IsNullOrEmpty(source.ImageName))
                target.ImageName = source.ImageName;
            target.IsPartial = source.IsPartial;
        }

        if (isL2)
        {
            // L2 contains: properties and collections
            target.Properties.AddRange(source.Properties);
            target.Collections.AddRange(source.Collections);
        }

        if (isCustom)
        {
            // Custom contains: interfaces, methods, custom properties
            foreach (var iface in source.ImplementedInterfaces)
            {
                if (!target.ImplementedInterfaces.Contains(iface))
                    target.ImplementedInterfaces.Add(iface);
            }
            target.Methods.AddRange(source.Methods);
            // Custom properties might be computed properties, add them too
            foreach (var prop in source.Properties)
            {
                if (!target.Properties.Any(p => p.Name == prop.Name))
                    target.Properties.Add(prop);
            }
        }
    }

    private string? GetClassNameFromPath(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        // Remove suffixes
        fileName = fileName.Replace(".generatorL1", "");
        fileName = fileName.Replace(".generatorL2", "");
        fileName = fileName.Replace(".objCompare", "");

        return fileName;
    }
}

