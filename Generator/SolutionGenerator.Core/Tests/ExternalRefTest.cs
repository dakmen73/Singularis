using Json.Schema;
using System.Text.Json;
using SolutionGenerator.Core.Validators;

namespace SolutionGenerator.Core.Tests;

public class ExternalRefTest
{
    public static async Task TestExternalReference()
    {
        Console.WriteLine("Testing external $ref to xpo_schema.json...");
        Console.WriteLine();

        // Cesta k testovacímu JSON souboru (relativně k workspace root)
        // Použijeme AppContext.BaseDirectory pro získání cesty k assembly
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? Directory.GetCurrentDirectory();
        
        // Najdeme workspace root (kde je solution.schema.json)
        var currentDir = new DirectoryInfo(assemblyDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "solution.schema.json")))
        {
            currentDir = currentDir.Parent;
        }
        
        if (currentDir == null)
        {
            Console.WriteLine("Error: Could not find workspace root (solution.schema.json)");
            return;
        }
        
        var workspaceRoot = currentDir.FullName;
        var testJsonPath = Path.Combine(workspaceRoot, "test_external_ref.json");
        var schemaPath = Path.Combine(workspaceRoot, "solution.schema.json");
        var xpoSchemaPath = Path.Combine(workspaceRoot, "xpo_schema.json");

        if (!File.Exists(testJsonPath))
        {
            Console.WriteLine($"Error: Test JSON file not found: {testJsonPath}");
            return;
        }

        if (!File.Exists(schemaPath))
        {
            Console.WriteLine($"Error: Schema file not found: {schemaPath}");
            return;
        }

        if (!File.Exists(xpoSchemaPath))
        {
            Console.WriteLine($"Error: XPO Schema file not found: {xpoSchemaPath}");
            return;
        }

        Console.WriteLine($"Test JSON: {testJsonPath}");
        Console.WriteLine($"Main Schema: {schemaPath}");
        Console.WriteLine($"XPO Schema: {xpoSchemaPath}");
        Console.WriteLine();

        try
        {
            // Načtení testovacího JSON
            var testJson = await File.ReadAllTextAsync(testJsonPath);
            var jsonDocument = JsonDocument.Parse(testJson);

            // Načtení XPO schématu a registrace pro externí reference
            Console.WriteLine("Loading XPO schema...");
            var xpoSchemaJson = await File.ReadAllTextAsync(xpoSchemaPath);
            var xpoSchema = JsonSchema.FromText(xpoSchemaJson);
            
            // Registrace XPO schématu pod jeho $id (z xpo_schema.json)
            var xpoSchemaId = new Uri("https://example.com/xpo-diagram.schema.json");
            SchemaRegistry.Global.Register(xpoSchemaId, xpoSchema);
            Console.WriteLine($"XPO schema registered under: {xpoSchemaId}");
            
            // Také registrujeme pod relativní cestou pro $ref: "xpo_schema.json#"
            // Musíme vytvořit URI z relativní cesty
            var schemaDir = Path.GetDirectoryName(Path.GetFullPath(schemaPath)) ?? workspaceRoot;
            var xpoSchemaFileUri = new Uri(Path.GetFullPath(xpoSchemaPath));
            SchemaRegistry.Global.Register(xpoSchemaFileUri, xpoSchema);
            Console.WriteLine($"XPO schema also registered under file URI: {xpoSchemaFileUri}");
            
            // Načtení hlavního schématu
            Console.WriteLine("Loading main schema...");
            var schemaJson = await File.ReadAllTextAsync(schemaPath);
            var schema = JsonSchema.FromText(schemaJson);

            Console.WriteLine("Schema loaded. Attempting validation...");
            Console.WriteLine();

            // Validace
            var results = schema.Evaluate(jsonDocument.RootElement);

            Console.WriteLine($"Validation result: {(results.IsValid ? "VALID ✓" : "INVALID ✗")}");
            Console.WriteLine();

            if (!results.IsValid)
            {
                Console.WriteLine("Validation errors:");
                CollectErrors(results, "", 0);
            }
            else
            {
                Console.WriteLine("✓ External reference to xpo_schema.json works correctly!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during validation: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static void CollectErrors(dynamic results, string path, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        
        if (!string.IsNullOrEmpty(results.Message))
        {
            var errorPath = string.IsNullOrEmpty(path) ? "root" : path;
            Console.WriteLine($"{indentStr}{errorPath}: {results.Message}");
        }

        if (results.NestedResults != null)
        {
            foreach (var nested in results.NestedResults)
            {
                var nestedPath = string.IsNullOrEmpty(path) ? nested.Key : $"{path}.{nested.Key}";
                CollectErrors(nested.Value, nestedPath, indent + 1);
            }
        }
    }
}

