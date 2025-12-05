using System.Collections.Generic;

namespace XpoExtractor.Models;

public class ClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string? BaseClass { get; set; }
    public List<string> ImplementedInterfaces { get; set; } = new();
    public bool IsPartial { get; set; }
    public string? Persistent { get; set; }
    public string? ImageName { get; set; }
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<CollectionInfo> Collections { get; set; } = new();
    public List<MethodInfo> Methods { get; set; } = new();
    public List<CustomAttributeInfo> CustomAttributes { get; set; } = new();
}

public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Persistent { get; set; }
    public string? ColumnType { get; set; }
    public string? VirtualColumnType { get; set; }
    public int? Size { get; set; }
    public bool IsKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsNullable { get; set; }
    public int? Index { get; set; }
    public bool DelayedUpdateModifiedOnly { get; set; }
    public List<CustomAttributeInfo> CustomAttributes { get; set; } = new();
}

public class CollectionInfo
{
    public string Name { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public string? AssociationName { get; set; }
    public bool IsAggregated { get; set; }
    public int? Index { get; set; }
    public string? DisplayName { get; set; }
    public string? Summary { get; set; }
}

public class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = "void";
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public List<ParameterInfo> Parameters { get; set; } = new();
}

public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class CustomAttributeInfo
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

