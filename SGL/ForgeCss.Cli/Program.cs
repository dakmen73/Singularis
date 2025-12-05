// ForgeCss.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ForgeCss.Core.Models;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ILogger>(sp =>
            sp.GetRequiredService<ILogger<Program>>());
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger>();

if (args.Length == 0 || args[0] != "apply")
{
    logger.LogError("Usage: forgecss apply <file.fcss> [--dry-run]");
    return 1;
}

var file = args[1];
var dryRun = args.Contains("--dry-run");

logger.LogInformation($"🚀 ForgeCSS starting...");
logger.LogInformation($"📁 Input file: {file}");

if (!File.Exists(file))
{
    logger.LogError($"❌ File not found: {file}");
    return 1;
}

try
{
    var content = await File.ReadAllTextAsync(file);
    logger.LogInformation($"📄 File content length: {content.Length} characters");

    // SKUTEČNÉ PARSOVÁNÍ PODLE EBNF GRAMATIKY!
    var stylesheet = CssLikeParser.Parse(content);
    logger.LogInformation($"✅ Parsed successfully: {stylesheet.Statements.Count} statements");

    // Zobrazte co bylo naparsováno
    DisplayParsedAst(stylesheet, logger);

    if (dryRun)
    {
        logger.LogInformation("[DRY-RUN] Would execute engine here");
    }
    else
    {
        logger.LogInformation("📁 Engine would create actual files here");
    }

    logger.LogInformation("🎉 Scaffolding completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Scaffolding failed");
    return 1;
}

return 0;

void DisplayParsedAst(Stylesheet sheet, ILogger log)
{
    log.LogInformation("📊 AST Structure:");
    foreach (var statement in sheet.Statements)
    {
        switch (statement)
        {
            case RootRule root:
                log.LogInformation("  :root with {0} declarations", root.Body.Count);
                break;
            case Rule rule:
                log.LogInformation("  Rule with selector: {0}", rule.Selector);
                break;
            case Declaration decl:
                log.LogInformation("  Declaration: {0} = {1}", decl.Name, decl.Value);
                break;
            case ExecRule exec:
                log.LogInformation("  Exec: {0}", exec.Command);
                break;
        }
    }
}