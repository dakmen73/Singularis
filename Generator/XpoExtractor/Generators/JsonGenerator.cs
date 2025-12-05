using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using XpoExtractor.Models;

namespace XpoExtractor.Generators;

public class JsonGenerator
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string GenerateJson(List<ClassInfo> classes)
    {
        var model = new Dictionary<string, object?>
        {
            ["type"] = "xpo",
            ["classes"] = classes.Select(c => ConvertClass(c)).ToList(),
            ["relationships"] = ExtractRelationships(classes),
            ["notes"] = new List<object>(),
            ["externalTypes"] = new List<object>()
        };

        return JsonSerializer.Serialize(model, _options);
    }

    private object ConvertClass(ClassInfo classInfo)
    {
        return new Dictionary<string, object?>
        {
            ["name"] = classInfo.Name,
            ["namespace"] = classInfo.Namespace,
            ["baseClass"] = classInfo.BaseClass,
            ["implementedInterfaces"] = classInfo.ImplementedInterfaces,
            ["persistent"] = classInfo.Persistent,
            ["imageName"] = classInfo.ImageName,
            ["nonPersistent"] = string.IsNullOrEmpty(classInfo.Persistent),
            ["attributes"] = classInfo.Properties.Select(p => ConvertProperty(p)).ToList(),
            ["collections"] = classInfo.Collections.Select(c => ConvertCollection(c)).ToList(),
            ["methods"] = classInfo.Methods.Select(m => ConvertMethod(m)).ToList(),
            ["customAttributes"] = classInfo.CustomAttributes.Select(a => ConvertCustomAttribute(a)).ToList()
        };
    }

    private object ConvertProperty(PropertyInfo prop)
    {
        var dict = new Dictionary<string, object?>
        {
            ["name"] = prop.Name,
            ["type"] = prop.Type,
            ["displayName"] = prop.DisplayName,
            ["persistent"] = prop.Persistent,
            ["columnType"] = prop.ColumnType,
            ["virtualColumnType"] = prop.VirtualColumnType,
            ["isKey"] = prop.IsKey,
            ["isIdentity"] = prop.IsIdentity,
            ["isNullable"] = prop.IsNullable,
            ["index"] = prop.Index,
            ["delayedUpdateModifiedOnly"] = prop.DelayedUpdateModifiedOnly,
            ["customAttributes"] = prop.CustomAttributes.Select(a => ConvertCustomAttribute(a)).ToList()
        };
        
        if (prop.Size.HasValue)
        {
            dict["size"] = prop.Size == -1 ? "Unlimited" : prop.Size;
        }
        
        return dict;
    }

    private object ConvertCollection(CollectionInfo collection)
    {
        return new Dictionary<string, object?>
        {
            ["name"] = collection.Name,
            ["elementType"] = collection.ElementType,
            ["associationName"] = collection.AssociationName,
            ["isAggregated"] = collection.IsAggregated,
            ["index"] = collection.Index,
            ["displayName"] = collection.DisplayName,
            ["summary"] = collection.Summary
        };
    }

    private object ConvertMethod(MethodInfo method)
    {
        return new Dictionary<string, object?>
        {
            ["name"] = method.Name,
            ["returnType"] = method.ReturnType,
            ["isStatic"] = method.IsStatic,
            ["isAbstract"] = method.IsAbstract,
            ["parameters"] = method.Parameters.Select(p => new Dictionary<string, object?>
            {
                ["name"] = p.Name,
                ["type"] = p.Type
            }).ToList()
        };
    }

    private object ConvertCustomAttribute(CustomAttributeInfo attr)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = attr.Type,
            ["parameters"] = attr.Parameters
        };
    }

    private List<object> ExtractRelationships(List<ClassInfo> classes)
    {
        var relationships = new List<object>();

        foreach (var classInfo in classes)
        {
            // Extract relationships from collections
            foreach (var collection in classInfo.Collections)
            {
                if (!string.IsNullOrEmpty(collection.AssociationName))
                {
                    relationships.Add(new Dictionary<string, object?>
                    {
                        ["type"] = collection.IsAggregated ? "xpo-aggregated" : "xpo-association",
                        ["from"] = classInfo.Name,
                        ["to"] = collection.ElementType,
                        ["associationName"] = collection.AssociationName,
                        ["sourceCollectionName"] = collection.Name,
                        ["index"] = collection.Index
                    });
                }
            }
        }

        // Extract inheritance relationships
        foreach (var classInfo in classes)
        {
            if (!string.IsNullOrEmpty(classInfo.BaseClass))
            {
                relationships.Add(new Dictionary<string, object?>
                {
                    ["type"] = "inheritance",
                    ["from"] = classInfo.Name,
                    ["to"] = classInfo.BaseClass
                });
            }
        }

        return relationships;
    }
}

