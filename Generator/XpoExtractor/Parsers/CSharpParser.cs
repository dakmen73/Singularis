using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using XpoExtractor.Models;

namespace XpoExtractor.Parsers;

public class CSharpParser
{
    public ClassInfo ParseClass(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var classInfo = new ClassInfo();

        // Parse namespace
        var namespaceMatch = Regex.Match(content, @"namespace\s+([\w.]+)");
        if (namespaceMatch.Success)
        {
            classInfo.Namespace = namespaceMatch.Groups[1].Value;
        }

        // Parse class declaration - improved to handle base class and interfaces separately
        var classMatch = Regex.Match(content, 
            @"(public\s+)?(partial\s+)?class\s+(\w+)(?:\s*:\s*([^\{]+))?", 
            RegexOptions.Singleline);
        if (classMatch.Success)
        {
            classInfo.Name = classMatch.Groups[3].Value;
            classInfo.IsPartial = classMatch.Groups[2].Success;
            
            var baseAndInterfaces = classMatch.Groups[4].Value?.Trim();
            if (!string.IsNullOrEmpty(baseAndInterfaces))
            {
                ParseBaseClassAndInterfaces(baseAndInterfaces, classInfo);
            }
        }

        // Parse [Persistent] attribute
        var persistentMatch = Regex.Match(content, 
            @"\[Persistent\(@?""([^""]+)""\)\]");
        if (persistentMatch.Success)
        {
            classInfo.Persistent = persistentMatch.Groups[1].Value;
        }

        // Parse [ImageName] attribute
        var imageNameMatch = Regex.Match(content, 
            @"\[ImageName\(@?""([^""]+)""\)\]");
        if (imageNameMatch.Success)
        {
            classInfo.ImageName = imageNameMatch.Groups[1].Value;
        }

        // Parse [DisplayName] attribute (System.ComponentModel.DisplayName)
        var displayNameMatch = Regex.Match(content, 
            @"\[(?:System\.ComponentModel\.)?DisplayName\(@?""([^""]+)""\)\]");
        if (displayNameMatch.Success)
        {
            // Store as custom attribute
            classInfo.CustomAttributes.Add(new CustomAttributeInfo
            {
                Type = "System.ComponentModel.DisplayName",
                Parameters = new Dictionary<string, object> { ["value"] = displayNameMatch.Groups[1].Value }
            });
        }

        // Parse properties (from .generatorL2.cs - SetPropertyValue pattern)
        ParseProperties(content, classInfo);

        // Parse collections (XPCollection pattern)
        ParseCollections(content, classInfo);

        // Parse methods (from .cs file)
        ParseMethods(content, classInfo);

        return classInfo;
    }

    private void ParseBaseClassAndInterfaces(string baseAndInterfaces, ClassInfo classInfo)
    {
        // Split by comma, but be careful with generic types and nested brackets
        var parts = new List<string>();
        var current = "";
        var depth = 0;
        var inString = false;
        var stringChar = '\0';
        
        for (int i = 0; i < baseAndInterfaces.Length; i++)
        {
            var c = baseAndInterfaces[i];
            if (!inString && (c == '"' || c == '@'))
            {
                inString = true;
                if (c == '@' && i + 1 < baseAndInterfaces.Length)
                {
                    var next = baseAndInterfaces[i + 1];
                    if (next == '"') 
                    {
                        stringChar = '"';
                        i++; // Skip the @
                    }
                }
                else stringChar = c;
            }
            else if (inString && c == stringChar)
            {
                inString = false;
            }
            
            if (!inString)
            {
                if (c == '<') depth++;
                else if (c == '>') depth--;
                else if (c == ',' && depth == 0)
                {
                    parts.Add(current.Trim());
                    current = "";
                    continue;
                }
            }
            current += c;
        }
        if (!string.IsNullOrEmpty(current))
            parts.Add(current.Trim());

        if (parts.Count > 0)
        {
            var first = parts[0];
            // Check if first part is an interface (starts with I and next char is uppercase)
            // But be careful - base classes can also start with I (like IList, but those are usually generic)
            // Better heuristic: if it contains a dot, it's likely a namespace-qualified base class
            // If it's a simple name starting with I and uppercase second char, it's likely an interface
            if (!first.Contains('.') && first.StartsWith("I") && first.Length > 1 && char.IsUpper(first[1]))
            {
                // First is interface, all are interfaces
                classInfo.ImplementedInterfaces.AddRange(parts);
            }
            else
            {
                // First is base class (could be namespace-qualified like GA.PIS.BO.PISObject)
                classInfo.BaseClass = first;
                // Rest are interfaces
                for (int i = 1; i < parts.Count; i++)
                {
                    classInfo.ImplementedInterfaces.Add(parts[i]);
                }
            }
        }
    }

    private void ParseProperties(string content, ClassInfo classInfo)
    {
        // Pattern for XPO properties: field declaration + property with SetPropertyValue
        // Example:
        // Guid fOID;
        // /// <summary>...</summary>
        // [Key(true)]
        // [Index(0)]
        // public Guid OID { get { return fOID; } set { SetPropertyValue<Guid>(nameof(OID), ref fOID, value); } }

        // Strategy: Find all SetPropertyValue calls and extract property name from nameof()
        // Pattern: SetPropertyValue<Type>(nameof(PropertyName), ref fFieldName, value);
        var setPropertyPattern = @"SetPropertyValue<([\w.]+(?:<[^>]+>)?)>\(nameof\((\w+)\),\s*ref\s+f(\w+)[^)]*\);";
        var setPropertyMatches = Regex.Matches(content, setPropertyPattern, RegexOptions.Singleline);

        foreach (Match setPropMatch in setPropertyMatches)
        {
            var propertyType = setPropMatch.Groups[1].Value;
            var propName = setPropMatch.Groups[2].Value;
            var fieldName = setPropMatch.Groups[3].Value;
            
            // Find the property declaration - look backwards from SetPropertyValue
            var setPropIndex = setPropMatch.Index;
            var lookBackStart = Math.Max(0, setPropIndex - 2000);
            var lookBackContent = content.Substring(lookBackStart, setPropIndex - lookBackStart);
            
            // Find the property declaration: public Type PropertyName {
            var propertyPattern = 
                @"public\s+([\w.]+(?:<[^>]+>)?)\s+" + Regex.Escape(propName) + @"\s*\{";
            
            var propMatch = Regex.Match(lookBackContent, propertyPattern, RegexOptions.Multiline);
            
            if (propMatch.Success)
            {
                var prop = new PropertyInfo
                {
                    Name = propName,
                    Type = propMatch.Groups[1].Value
                };

                // Extract attributes - find the section between previous property/field and current property
                var propertyAbsoluteIndex = lookBackStart + propMatch.Index;
                
                // Find the start of attributes section - look for previous property or field declaration
                var beforeProperty = content.Substring(Math.Max(0, propertyAbsoluteIndex - 1000), 
                    Math.Min(1000, propertyAbsoluteIndex));
                
                // Find the last property or field declaration before this one
                var prevPropertyMatch = Regex.Match(beforeProperty, 
                    @"(?:public\s+[\w.]+(?:<[^>]+>)?\s+\w+\s*\{|[\w.]+\s+f\w+\s*;)", 
                    RegexOptions.RightToLeft | RegexOptions.Multiline);
                
                var attrStart = prevPropertyMatch.Success 
                    ? propertyAbsoluteIndex - 1000 + prevPropertyMatch.Index + prevPropertyMatch.Length
                    : Math.Max(0, propertyAbsoluteIndex - 1000);
                
                // Property section: from attrStart to end of SetPropertyValue call
                var propertySection = content.Substring(attrStart, 
                    setPropIndex + setPropMatch.Length - attrStart);
                
                ParsePropertyAttributes(propertySection, prop);

                classInfo.Properties.Add(prop);
            }
        }
    }

    private void ParsePropertyAttributes(string propertySection, PropertyInfo prop)
    {
        // Parse [Key(true)]
        var keyMatch = Regex.Match(propertySection, @"\[Key\(true\)\]");
        if (keyMatch.Success)
        {
            prop.IsKey = true;
        }

        // Parse [Index(0)]
        var indexMatch = Regex.Match(propertySection, @"\[Index\((\d+)\)\]");
        if (indexMatch.Success)
        {
            prop.Index = int.Parse(indexMatch.Groups[1].Value);
        }

        // Parse [Size(100)] or [Size(Unlimited)] or [Size(SizeAttribute.Unlimited)]
        var sizeMatch = Regex.Match(propertySection, @"\[Size\((\d+|Unlimited|SizeAttribute\.Unlimited)\)\]");
        if (sizeMatch.Success)
        {
            var sizeValue = sizeMatch.Groups[1].Value;
            if (sizeValue == "Unlimited" || sizeValue == "SizeAttribute.Unlimited")
                prop.Size = -1;
            else
                prop.Size = int.Parse(sizeValue);
        }

        // Parse [DevExpress.Xpo.DisplayName(@"...")]
        var displayNameMatch = Regex.Match(propertySection, 
            @"\[DevExpress\.Xpo\.DisplayName\(@?""([^""]+)""\)\]");
        if (displayNameMatch.Success)
        {
            prop.DisplayName = displayNameMatch.Groups[1].Value;
        }

        // Parse [Association(@"...")] - this indicates a reference property
        var associationMatch = Regex.Match(propertySection, 
            @"\[Association\(@?""([^""]+)""\)\]");
        if (associationMatch.Success)
        {
            // Store as custom attribute - this can be used to identify reference relationships
            prop.CustomAttributes.Add(new CustomAttributeInfo
            {
                Type = "Association",
                Parameters = new Dictionary<string, object> { ["name"] = associationMatch.Groups[1].Value }
            });
        }
        
        // Parse [Aggregated] - indicates aggregated reference
        var aggregatedMatch = Regex.Match(propertySection, @"\[Aggregated(?:Attribute)?\]");
        if (aggregatedMatch.Success)
        {
            prop.CustomAttributes.Add(new CustomAttributeInfo
            {
                Type = "Aggregated",
                Parameters = new Dictionary<string, object>()
            });
        }

        // Parse other custom attributes
        var attributeMatches = Regex.Matches(propertySection, 
            @"\[([\w.]+)(?:\(([^\)]+)\))?\]");
        foreach (Match attrMatch in attributeMatches)
        {
            var attrType = attrMatch.Groups[1].Value;
            var attrParams = attrMatch.Groups[2].Value;
            
            // Skip already parsed attributes
            if (attrType == "Key" || attrType == "Index" || attrType == "Size" || 
                attrType == "DevExpress.Xpo.DisplayName" || attrType == "Association" ||
                attrType == "Aggregated" || attrType == "AggregatedAttribute")
                continue;

            var customAttr = new CustomAttributeInfo { Type = attrType };
            
            if (!string.IsNullOrEmpty(attrParams))
            {
                // Parse parameters (key-value pairs)
                // Example: ModelDefault("DisplayFormat", @"G")
                // Example: ModelDefault("DisplayFormat", @"{0:N0} cm")
                var paramMatch = Regex.Match(attrParams, 
                    @"([\w""]+)\s*,\s*@?""([^""]+)""");
                if (paramMatch.Success)
                {
                    var key = paramMatch.Groups[1].Value.Trim('"');
                    var value = paramMatch.Groups[2].Value;
                    customAttr.Parameters[key] = value;
                }
                else
                {
                    // Try single quoted value
                    var singleMatch = Regex.Match(attrParams, @"@?""([^""]+)""");
                    if (singleMatch.Success)
                    {
                        customAttr.Parameters["value"] = singleMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Boolean or numeric value
                        customAttr.Parameters["value"] = attrParams.Trim();
                    }
                }
            }
            
            prop.CustomAttributes.Add(customAttr);
        }
    }

    private void ParseCollections(string content, ClassInfo classInfo)
    {
        // Pattern for XPCollection properties
        // Example:
        // /// <summary>
        // /// Záznamy DPK
        // /// </summary>
        // [Index(0)]
        // [Association(@"DPKRefHP")]
        // [DevExpress.Xpo.DisplayName(@"Záznamy DPK")]
        // public XPCollection<PIS_DPK_HP> c_DPK_HP
        // {
        //     get
        //     {
        //         return GetCollection<PIS_DPK_HP>(nameof(c_DPK_HP));
        //     }
        // }

        var collectionPattern = 
            @"(?:///\s*<summary>\s*(.*?)\s*///\s*</summary>\s*)?" +
            @"(?:\[[^\]]+\]\s*)*" +
            @"public\s+XPCollection<([\w.]+)>\s+(\w+)\s*" +
            @"\{[^}]*GetCollection<([\w.]+)>\([^)]+\);[^}]*\}";

        var matches = Regex.Matches(content, collectionPattern, 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var collection = new CollectionInfo
            {
                Name = match.Groups[3].Value,
                ElementType = match.Groups[4].Value // Use the type from GetCollection
            };

            if (match.Groups[1].Success)
            {
                var summary = match.Groups[1].Value.Trim();
                collection.Summary = summary;
            }

            // Extract attributes before the collection
            var collectionStart = match.Index;
            var beforeCollection = content.Substring(Math.Max(0, collectionStart - 500), 
                Math.Min(500, collectionStart));
            
            // Parse [Index(0)]
            var indexMatch = Regex.Match(beforeCollection, @"\[Index\((\d+)\)\]");
            if (indexMatch.Success)
            {
                collection.Index = int.Parse(indexMatch.Groups[1].Value);
            }

            // Parse [Association(@"...")]
            var associationMatch = Regex.Match(beforeCollection, 
                @"\[Association\(@?""([^""]+)""\)\]");
            if (associationMatch.Success)
            {
                collection.AssociationName = associationMatch.Groups[1].Value;
            }

            // Parse [Aggregated] or [AggregatedAttribute]
            var aggregatedMatch = Regex.Match(beforeCollection, @"\[Aggregated(?:Attribute)?\]");
            if (aggregatedMatch.Success)
            {
                collection.IsAggregated = true;
            }

            // Parse [DevExpress.Xpo.DisplayName(@"...")]
            var displayNameMatch = Regex.Match(beforeCollection, 
                @"\[DevExpress\.Xpo\.DisplayName\(@?""([^""]+)""\)\]");
            if (displayNameMatch.Success)
            {
                collection.DisplayName = displayNameMatch.Groups[1].Value;
            }

            classInfo.Collections.Add(collection);
        }
    }

    private void ParseMethods(string content, ClassInfo classInfo)
    {
        // Improved method pattern - handle multiline and various modifiers
        var methodPattern = 
            @"(public|private|protected|internal)\s+" +
            @"(static\s+)?(abstract\s+)?(override\s+)?(virtual\s+)?" +
            @"(\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)";

        var matches = Regex.Matches(content, methodPattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            var method = new MethodInfo
            {
                Name = match.Groups[7].Value,
                ReturnType = match.Groups[6].Value,
                IsStatic = match.Groups[2].Success,
                IsAbstract = match.Groups[3].Success
            };

            // Parse parameters
            var paramsStr = match.Groups[8].Value;
            if (!string.IsNullOrWhiteSpace(paramsStr))
            {
                var paramParts = paramsStr.Split(',');
                foreach (var paramPart in paramParts)
                {
                    var trimmed = paramPart.Trim();
                    // Handle ref, out, params modifiers
                    trimmed = Regex.Replace(trimmed, @"^(ref|out|params)\s+", "");
                    var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        method.Parameters.Add(new ParameterInfo
                        {
                            Type = parts[0],
                            Name = parts[1]
                        });
                    }
                }
            }

            classInfo.Methods.Add(method);
        }
    }
}
