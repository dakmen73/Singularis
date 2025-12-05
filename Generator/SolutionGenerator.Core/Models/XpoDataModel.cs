using System.Text.Json.Serialization;

namespace SolutionGenerator.Core.Models;

public class XpoDataModel
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("classes")]
    public List<XpoClass> Classes { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<XpoRelationship>? Relationships { get; set; }

    [JsonPropertyName("notes")]
    public List<XpoNote>? Notes { get; set; }

    [JsonPropertyName("externalTypes")]
    public List<XpoExternalType>? ExternalTypes { get; set; }
}

public class XpoClass
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("baseClass")]
    public string? BaseClass { get; set; }

    [JsonPropertyName("virtualBaseClass")]
    public string? VirtualBaseClass { get; set; }

    [JsonPropertyName("implementedInterfaces")]
    public List<string>? ImplementedInterfaces { get; set; }

    [JsonPropertyName("persistent")]
    public string? Persistent { get; set; }

    [JsonPropertyName("nonPersistent")]
    public bool NonPersistent { get; set; }

    [JsonPropertyName("attributes")]
    public List<XpoAttribute> Attributes { get; set; } = new();

    [JsonPropertyName("collections")]
    public List<XpoCollection> Collections { get; set; } = new();

    [JsonPropertyName("methods")]
    public List<XpoMethod> Methods { get; set; } = new();

    [JsonPropertyName("customAttributes")]
    public List<XpoCustomAttribute>? CustomAttributes { get; set; }

    [JsonPropertyName("imageName")]
    public string? ImageName { get; set; }
}

public class XpoAttribute
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("persistent")]
    public bool? Persistent { get; set; }

    [JsonPropertyName("columnName")]
    public string? ColumnName { get; set; }

    [JsonPropertyName("columnType")]
    public string? ColumnType { get; set; }

    [JsonPropertyName("virtualColumnType")]
    public string? VirtualColumnType { get; set; }

    [JsonPropertyName("isKey")]
    public bool IsKey { get; set; }

    [JsonPropertyName("isIdentity")]
    public bool IsIdentity { get; set; }

    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; set; }

    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("size")]
    public object? Size { get; set; } // int nebo "Unlimited"

    [JsonPropertyName("delayedUpdateModifiedOnly")]
    public bool DelayedUpdateModifiedOnly { get; set; }

    [JsonPropertyName("customAttributes")]
    public List<XpoCustomAttribute> CustomAttributes { get; set; } = new();
}

public class XpoCollection
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("elementType")]
    public string ElementType { get; set; } = string.Empty;

    [JsonPropertyName("associationName")]
    public string? AssociationName { get; set; }

    [JsonPropertyName("sourceCollectionName")]
    public string? SourceCollectionName { get; set; }

    [JsonPropertyName("targetFieldName")]
    public string? TargetFieldName { get; set; }

    [JsonPropertyName("isAggregated")]
    public bool IsAggregated { get; set; }

    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

public class XpoMethod
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("returnType")]
    public string ReturnType { get; set; } = "void";

    [JsonPropertyName("isStatic")]
    public bool IsStatic { get; set; }

    [JsonPropertyName("isAbstract")]
    public bool IsAbstract { get; set; }

    [JsonPropertyName("parameters")]
    public List<XpoParameter> Parameters { get; set; } = new();
}

public class XpoParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class XpoCustomAttribute
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

public class XpoRelationship
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("associationName")]
    public string? AssociationName { get; set; }
}

public class XpoNote
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("className")]
    public string ClassName { get; set; } = string.Empty;
}

public class XpoExternalType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;
}

