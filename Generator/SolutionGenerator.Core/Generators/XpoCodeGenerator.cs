using Scriban;
using Scriban.Runtime;
using SolutionGenerator.Core.Models;
using System.Text;
using System.Text.Json;

namespace SolutionGenerator.Core.Generators;

public class XpoCodeGenerator
{
    private readonly string _templatesDirectory;
    private readonly string _outputDirectory;

    public XpoCodeGenerator(string templatesDirectory, string outputDirectory)
    {
        _templatesDirectory = templatesDirectory;
        _outputDirectory = outputDirectory;
        
        // Vytvoření výstupního adresáře, pokud neexistuje
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task<GenerationResult> GenerateFromJsonAsync(string jsonFilePath)
    {
        var result = new GenerationResult();

        try
        {
            // Načtení JSON
            var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            var dataModel = JsonSerializer.Deserialize<XpoDataModel>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dataModel == null || dataModel.Classes == null)
            {
                result.Errors.Add("Invalid JSON structure or no classes found");
                return result;
            }

            // Načtení šablon
            var templateL1 = await LoadTemplateAsync("generatorL1.sbn");
            var templateL2 = await LoadTemplateAsync("generatorL2.sbn");
            var templateMain = await LoadTemplateAsync("main.sbn");

            if (templateL1 == null || templateL2 == null || templateMain == null)
            {
                result.Errors.Add("Failed to load templates");
                return result;
            }

            // Generování pro každou třídu
            foreach (var xpoClass in dataModel.Classes)
            {
                try
                {
                    var className = xpoClass.Name;
                    var namespaceName = xpoClass.Namespace ?? "Unknown";

                    // Vytvoření script object pro Scriban
                    var scriptObject = CreateScriptObject(xpoClass, dataModel);

                    // Generování .generatorL1.cs
                    var contentL1 = await templateL1.RenderAsync(scriptObject);
                    var fileL1 = Path.Combine(_outputDirectory, $"{className}.generatorL1.cs");
                    await File.WriteAllTextAsync(fileL1, contentL1, Encoding.UTF8);
                    result.GeneratedFiles.Add(fileL1);

                    // Generování .generatorL2.cs
                    var contentL2 = await templateL2.RenderAsync(scriptObject);
                    var fileL2 = Path.Combine(_outputDirectory, $"{className}.generatorL2.cs");
                    await File.WriteAllTextAsync(fileL2, contentL2, Encoding.UTF8);
                    result.GeneratedFiles.Add(fileL2);

                    // Generování .cs
                    var contentMain = await templateMain.RenderAsync(scriptObject);
                    var fileMain = Path.Combine(_outputDirectory, $"{className}.cs");
                    await File.WriteAllTextAsync(fileMain, contentMain, Encoding.UTF8);
                    result.GeneratedFiles.Add(fileMain);

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error generating code for class {xpoClass.Name}: {ex.Message}");
                }
            }

            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error processing JSON file: {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    private async Task<Template?> LoadTemplateAsync(string templateName)
    {
        var templatePath = Path.Combine(_templatesDirectory, templateName);
        
        if (!File.Exists(templatePath))
        {
            return null;
        }

        var templateContent = await File.ReadAllTextAsync(templatePath);
        return Template.Parse(templateContent);
    }

    private ScriptObject CreateScriptObject(XpoClass xpoClass, XpoDataModel dataModel)
    {
        var scriptObject = new ScriptObject();
        
        // Přidání třídy jako dictionary pro lepší přístup v šablonách
        var classDict = new ScriptObject();
        classDict["name"] = xpoClass.Name;
        classDict["namespace"] = xpoClass.Namespace ?? "";
        classDict["base_class"] = xpoClass.BaseClass ?? "";
        classDict["persistent"] = xpoClass.Persistent;
        classDict["custom_attributes"] = xpoClass.CustomAttributes ?? new List<XpoCustomAttribute>();
        classDict["attributes"] = xpoClass.Attributes;
        classDict["collections"] = xpoClass.Collections;
        classDict["methods"] = xpoClass.Methods;
        
        scriptObject["class"] = classDict;
        
        // Přidání helper funkcí
        scriptObject.Import("get_size_attribute", new Func<object?, string>(GetSizeAttribute));
        scriptObject.Import("get_custom_attributes", new Func<object, string>(GetCustomAttributesForTemplate));
        scriptObject.Import("get_using_statements", new Func<ScriptObject, string>(GetUsingStatementsForTemplate));
        scriptObject.Import("format_attributes_inline", new Func<object?, int?, bool, object?, string?, string>(
            (attrs, idx, key, sz, disp) => FormatAttributesInline(attrs, idx, key, sz, disp)));
        
        return scriptObject;
    }

    private string GetSizeAttribute(object? size)
    {
        if (size == null) return "";
        
        if (size is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String && jsonElement.GetString() == "Unlimited")
            {
                return "[Size(Unlimited)]";
            }
            if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                return $"[Size({jsonElement.GetInt32()})]";
            }
        }
        
        if (size is string str && str == "Unlimited")
        {
            return "[Size(Unlimited)]";
        }
        
        if (size is int intSize)
        {
            return $"[Size({intSize})]";
        }
        
        return "";
    }

    private string GetCustomAttributesForTemplate(object? attributesObj)
    {
        if (attributesObj == null) return "";
        
        List<XpoCustomAttribute>? attributes = null;
        
        if (attributesObj is List<XpoCustomAttribute> list)
        {
            attributes = list;
        }
        else if (attributesObj is ScriptArray scriptArray)
        {
            attributes = new List<XpoCustomAttribute>();
            foreach (var item in scriptArray)
            {
                if (item is ScriptObject so)
                {
                    var attr = new XpoCustomAttribute
                    {
                        Type = so.GetSafeValue<string>("type") ?? "",
                        Parameters = new Dictionary<string, object>()
                    };
                    
                    if (so.ContainsKey("parameters") && so["parameters"] is ScriptObject paramsObj)
                    {
                        foreach (var key in paramsObj.Keys)
                        {
                            attr.Parameters[key] = paramsObj[key] ?? "";
                        }
                    }
                    attributes.Add(attr);
                }
            }
        }
        
        if (attributes == null || attributes.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var attr in attributes)
        {
            sb.Append($"[{attr.Type}");
            
            if (attr.Parameters != null && attr.Parameters.Count > 0)
            {
                var paramsList = new List<string>();
                foreach (var param in attr.Parameters)
                {
                    var value = FormatAttributeParameter(param.Value);
                    // Pokud je parametr "value" a hodnota je boolean nebo string, použij zkrácenou syntaxi
                    if (param.Key == "value" && (param.Value is bool || param.Value is string))
                    {
                        paramsList.Add(value);
                    }
                    else
                    {
                        paramsList.Add($"{param.Key} = {value}");
                    }
                }
                sb.Append($"({string.Join(", ", paramsList)})");
            }
            
            sb.AppendLine("]");
        }
        
        return sb.ToString();
    }
    
    // Helper funkce pro formátování atributů na jeden řádek (pro generatorL2)
    private string FormatAttributesInline(object? attributesObj, int? index, bool isKey, object? size, string? displayName)
    {
        var attrs = new List<string>();
        
        // Custom attributes - zpracovat jednotlivě pro správné formátování
        if (attributesObj != null)
        {
            List<XpoCustomAttribute>? customAttrsList = null;
            
            if (attributesObj is List<XpoCustomAttribute> list)
            {
                customAttrsList = list;
            }
            else if (attributesObj is ScriptArray scriptArray)
            {
                customAttrsList = new List<XpoCustomAttribute>();
                foreach (var item in scriptArray)
                {
                    if (item is ScriptObject so)
                    {
                        var attr = new XpoCustomAttribute
                        {
                            Type = so.GetSafeValue<string>("type") ?? "",
                            Parameters = new Dictionary<string, object>()
                        };
                        
                        if (so.ContainsKey("parameters") && so["parameters"] is ScriptObject paramsObj)
                        {
                            foreach (var key in paramsObj.Keys)
                            {
                                attr.Parameters[key] = paramsObj[key] ?? "";
                            }
                        }
                        customAttrsList.Add(attr);
                    }
                }
            }
            
            if (customAttrsList != null)
            {
                foreach (var attr in customAttrsList)
                {
                    var attrStr = $"[{attr.Type}";
                    if (attr.Parameters != null && attr.Parameters.Count > 0)
                    {
                        var paramsList = new List<string>();
                        foreach (var param in attr.Parameters)
                        {
                            var value = FormatAttributeParameter(param.Value);
                            // Pokud je parametr "value" a hodnota je boolean nebo string, použij zkrácenou syntaxi
                            if (param.Key == "value")
                            {
                                // Pro boolean: false místo value = false
                                // Pro string: "text" místo value = "text"
                                paramsList.Add(value);
                            }
                            else
                            {
                                paramsList.Add($"{param.Key} = {value}");
                            }
                        }
                        attrStr += $"({string.Join(", ", paramsList)})";
                    }
                    attrStr += "]";
                    attrs.Add(attrStr);
                }
            }
        }
        
        // Index
        if (index != null)
        {
            attrs.Add($"[Index({index})]");
        }
        
        // Key
        if (isKey)
        {
            attrs.Add("[Key(true)]");
        }
        
        // Size
        if (size != null)
        {
            var sizeAttr = GetSizeAttribute(size);
            if (!string.IsNullOrEmpty(sizeAttr))
            {
                attrs.Add(sizeAttr.Trim());
            }
        }
        
        // DisplayName
        if (!string.IsNullOrEmpty(displayName))
        {
            attrs.Add($"[DevExpress.Xpo.DisplayName(@\"{displayName}\")]");
        }
        
        // Každý atribut na samostatný řádek s odsazením (jako v originálu)
        if (attrs.Count == 0) return "";
        return "        " + string.Join(Environment.NewLine + "        ", attrs);
    }
    
    private string GetUsingStatementsForTemplate(ScriptObject classObj)
    {
        // Pořadí podle originálu (generatorL1)
        var usings = new List<string>
        {
            "System",
            "System.Linq",
            "DevExpress.Xpo",
            "DevExpress.Xpo.Metadata",
            "DevExpress.Data.Filtering",
            "System.Collections.Generic",
            "System.ComponentModel",
            "System.Reflection",
            "DevExpress.Persistent.Base",
            "DevExpress.ExpressApp.Editors",
            "DevExpress.ExpressApp.Model",
            "DevExpress.Persistent.Validation",
            "DevExpress.ExpressApp.ConditionalAppearance"
        };

        // Přidání namespace z třídy (pokud existuje)
        var namespaceValue = classObj.GetSafeValue<string>("namespace");
        if (!string.IsNullOrEmpty(namespaceValue))
        {
            var parts = namespaceValue.Split('.');
            if (parts.Length > 0)
            {
                var baseNamespace = string.Join(".", parts.Take(parts.Length - 1));
                if (!string.IsNullOrEmpty(baseNamespace))
                {
                    // Přidat na konec, pokud ještě není v seznamu
                    if (!usings.Contains(baseNamespace))
                    {
                        usings.Add(baseNamespace);
                    }
                }
            }
        }

        return string.Join(Environment.NewLine, usings.Select(u => $"using {u};"));
    }

    private string FormatAttributeParameter(object? value)
    {
        if (value == null) return "null";
        
        if (value is string str)
        {
            return $"\"{str}\"";
        }
        
        if (value is bool b)
        {
            return b ? "true" : "false";
        }
        
        return value.ToString() ?? "null";
    }

    private string GetUsingStatements(XpoClass xpoClass)
    {
        var usings = new HashSet<string>
        {
            "System",
            "System.Linq",
            "DevExpress.Xpo",
            "DevExpress.Xpo.Metadata",
            "DevExpress.Data.Filtering",
            "System.Collections.Generic",
            "System.ComponentModel",
            "System.Reflection",
            "DevExpress.Persistent.Base",
            "DevExpress.ExpressApp.Editors",
            "DevExpress.ExpressApp.Model",
            "DevExpress.Persistent.Validation",
            "DevExpress.ExpressApp.ConditionalAppearance"
        };

        // Přidání namespace z třídy
        if (!string.IsNullOrEmpty(xpoClass.Namespace))
        {
            var parts = xpoClass.Namespace.Split('.');
            if (parts.Length > 0)
            {
                usings.Add(string.Join(".", parts.Take(parts.Length - 1)));
            }
        }

        return string.Join(Environment.NewLine, usings.OrderBy(u => u).Select(u => $"using {u};"));
    }
}

public class GenerationResult
{
    public bool Success { get; set; }
    public List<string> GeneratedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int SuccessCount { get; set; }
}

