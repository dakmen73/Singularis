using System.CommandLine;
using XpoExtractor.Extractors;
using XpoExtractor.Generators;

var inputOption = new Option<string>(
    "--input",
    description: "Cesta k adresáři s C# soubory business objektů")
{
    IsRequired = true
};

var outputOption = new Option<string>(
    "--output",
    description: "Cesta k výstupnímu JSON souboru")
{
    IsRequired = true
};

var rootCommand = new RootCommand("XPO Extractor - extrahuje strukturu business objektů do JSON")
{
    inputOption,
    outputOption
};

rootCommand.SetHandler(async (input, output) =>
{
    try
    {
        Console.WriteLine($"Extrahování z: {input}");
        
        var extractor = new XpoExtractor.Extractors.XpoExtractor();
        var classes = extractor.ExtractFromDirectory(input);
        
        Console.WriteLine($"Nalezeno {classes.Count} tříd");
        
        var generator = new JsonGenerator();
        var json = generator.GenerateJson(classes);
        
        await File.WriteAllTextAsync(output, json);
        
        Console.WriteLine($"JSON uložen do: {output}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Chyba: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
        Environment.Exit(1);
    }
}, inputOption, outputOption);

await rootCommand.InvokeAsync(args);

