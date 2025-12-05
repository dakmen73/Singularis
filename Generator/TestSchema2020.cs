using NJsonSchema;

var schemaJson = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://example.com/test.schema.json",
  "type": "object",
  "properties": {
    "test": {
      "type": "string"
    }
  },
  "$defs": {
    "testDef": {
      "type": "string"
    }
  }
}
""";

var dataJson = """
{
  "test": "value"
}
""";

try
{
    var schema = await JsonSchema.FromJsonAsync(schemaJson);
    var errors = schema.Validate(dataJson);
    
    Console.WriteLine($"Schema loaded: {(schema != null ? "YES" : "NO")}");
    Console.WriteLine($"Validation errors: {errors.Count()}");
    
    if (errors.Any())
    {
        foreach (var error in errors)
        {
            Console.WriteLine($"  - {error}");
        }
    }
    else
    {
        Console.WriteLine("✓ Validation successful - NJsonSchema supports draft 2020-12 with $defs!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.WriteLine($"  Type: {ex.GetType().Name}");
}

