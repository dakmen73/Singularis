using Json.Schema;
using System.Text.Json;
using SolutionGenerator.Core.Models;

namespace SolutionGenerator.Core.Validators;

public class JsonSchemaValidator
{
    private readonly string _schemaPath;

    public JsonSchemaValidator(string schemaPath)
    {
        _schemaPath = schemaPath;
    }

    public async Task<ValidationResult> ValidateAsync(string jsonContent)
    {
        if (string.IsNullOrEmpty(_schemaPath) || !File.Exists(_schemaPath))
        {
            // Pokud schema neexistuje, přeskočíme validaci
            return new ValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            };
        }

        try
        {
            // Načtení schématu ze souboru
            var schemaJson = await File.ReadAllTextAsync(_schemaPath);
            var schema = JsonSchema.FromText(schemaJson);
            
            // Parsování JSON pro validaci
            var jsonDocument = JsonDocument.Parse(jsonContent);
            
            // Validace
            var results = schema.Evaluate(jsonDocument.RootElement);

            var errors = new List<string>();
            if (!results.IsValid)
            {
                // Sběr chyb z výsledků - JsonSchema.Net vrací chyby v Message property
                CollectErrors(results, errors, "");
            }

            return new ValidationResult
            {
                IsValid = results.IsValid,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            // Pokud validace selže, vrátíme chybu
            Console.WriteLine($"Error: Schema validation failed: {ex.Message}");
            Console.WriteLine($"  Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner exception: {ex.InnerException.Message}");
            }
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Validation error: {ex.Message}" }
            };
        }
    }

    private void CollectErrors(dynamic results, List<string> errors, string path)
    {
        // JsonSchema.Net vrací ValidationResults s Message a NestedResults
        if (results.Message != null)
        {
            var errorPath = string.IsNullOrEmpty(path) ? "root" : path;
            errors.Add($"{errorPath}: {results.Message}");
        }
        
        if (results.NestedResults != null)
        {
            foreach (var nested in results.NestedResults)
            {
                var nestedPath = string.IsNullOrEmpty(path) ? nested.Key : $"{path}.{nested.Key}";
                CollectErrors(nested.Value, errors, nestedPath);
            }
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

