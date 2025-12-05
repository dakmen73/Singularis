using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<ICommandRunner, WhitelistCommandRunner>();
        services.AddSingleton<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddSingleton<CssLikeParser>();
        services.AddTransient<ScaffoldingEngine>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var parser = host.Services.GetRequiredService<CssLikeParser>();
var engine = host.Services.GetRequiredService<ScaffoldingEngine>();

try
{
    var inputFile = args[0];
    var dryRun = args.Contains("--dry-run");
    
    var content = await File.ReadAllTextAsync(inputFile);
    var stylesheet = parser.Parse(content);
    
    await engine.ExecuteAsync(stylesheet);
    
    logger.LogInformation("Scaffolding completed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Scaffolding failed");
    return 1;
}

return 0;