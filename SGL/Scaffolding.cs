public class ScaffoldingEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly ICommandRunner _commandRunner;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger _logger;
    private readonly bool _dryRun;
    
    private Scope _currentScope = Scope.CreateRoot();
    private string _currentDirectory;
    private readonly Stack<string> _directoryStack = new();

    public ScaffoldingEngine(
        IFileSystem fileSystem,
        ICommandRunner commandRunner, 
        ITemplateRenderer templateRenderer,
        ILogger logger,
        bool dryRun = false)
    {
        _fileSystem = fileSystem;
        _commandRunner = commandRunner;
        _templateRenderer = templateRenderer;
        _logger = logger;
        _dryRun = dryRun;
        _currentDirectory = _fileSystem.GetCurrentDirectory();
    }

    public async Task ExecuteAsync(Stylesheet stylesheet)
    {
        foreach (var statement in stylesheet.Statements)
        {
            await ExecuteStatementAsync(statement, _currentDirectory);
        }
    }

    private async Task ExecuteStatementAsync(Statement statement, string basePath)
    {
        switch (statement)
        {
            case RootRule root:
                _currentScope.Push();
                foreach (var stmt in root.Body)
                    await ExecuteStatementAsync(stmt, basePath);
                _currentScope.Pop();
                break;
                
            case Declaration decl:
                var value = await EvaluateValueAsync(decl.Value);
                _currentScope.SetVariable(decl.Name, value);
                break;
                
            case Rule rule:
                await ExecuteRuleAsync(rule, basePath);
                break;
                
            case IfRule ifRule:
                var condition = await EvaluateValueAsync(ifRule.Condition);
                if (ToBool(condition))
                {
                    foreach (var stmt in ifRule.ThenBranch)
                        await ExecuteStatementAsync(stmt, basePath);
                }
                else if (ifRule.ElseBranch != null)
                {
                    foreach (var stmt in ifRule.ElseBranch)
                        await ExecuteStatementAsync(stmt, basePath);
                }
                break;
                
            case ForeachRule foreachRule:
                var sequence = await EvaluateValueAsync(foreachRule.Sequence);
                if (sequence is ArrayValue array)
                {
                    foreach (var item in array.Items)
                    {
                        _currentScope.Push();
                        _currentScope.SetVariable(foreachRule.Variable, item);
                        foreach (var stmt in foreachRule.Body)
                            await ExecuteStatementAsync(stmt, basePath);
                        _currentScope.Pop();
                    }
                }
                break;
                
            case ExecRule execRule:
                await ExecuteCommandAsync(execRule, basePath);
                break;
                
            case MixinDecl mixinDecl:
                _currentScope.DefineMixin(mixinDecl.Name, mixinDecl);
                break;
                
            case MixinInclude mixinInclude:
                await IncludeMixinAsync(mixinInclude, basePath);
                break;
        }
    }

    private async Task ExecuteRuleAsync(Rule rule, string basePath)
    {
        var targetPath = await ResolveSelectorAsync(rule.Selector, basePath);
        
        if (rule.Selector is PathSelector)
        {
            // Directory rule
            if (!_dryRun)
                _fileSystem.EnsureDirectoryExists(targetPath);
            
            _logger.LogInformation($"Directory: {targetPath}");
            
            _directoryStack.Push(_currentDirectory);
            _currentDirectory = targetPath;
            
            try
            {
                foreach (var statement in rule.Body)
                    await ExecuteStatementAsync(statement, targetPath);
            }
            finally
            {
                _currentDirectory = _directoryStack.Pop();
            }
        }
        else if (rule.Selector is FileSelector fileSelector)
        {
            // File rule
            await ProcessFileRuleAsync(fileSelector, rule.Body, targetPath);
        }
    }

    private async Task ProcessFileRuleAsync(FileSelector fileSelector, IReadOnlyList<Statement> body, string filePath)
    {
        var fileContext = new FileContext();
        
        // Collect file properties from declarations
        foreach (var statement in body)
        {
            if (statement is Declaration decl)
            {
                fileContext.SetProperty(decl.Name, await EvaluateValueAsync(decl.Value));
            }
            else
            {
                await ExecuteStatementAsync(statement, Path.GetDirectoryName(filePath)!);
            }
        }
        
        await ApplyFileAsync(filePath, fileContext);
    }

    private async Task ApplyFileAsync(string filePath, FileContext context)
    {
        string content;
        
        if (context.Template != null)
        {
            var model = await BuildTemplateModelAsync(context);
            content = await _templateRenderer.RenderTemplateAsync(context.Template, model);
        }
        else if (context.Content != null)
        {
            content = await RenderContentAsync(context.Content);
        }
        else
        {
            throw new InvalidOperationException($"File {filePath} has no content or template");
        }

        var mode = context.Mode ?? "overwrite-if-changed";
        await WriteFileIfChangedAsync(filePath, content, mode);
    }

    private async Task WriteFileIfChangedAsync(string filePath, string newContent, string mode)
    {
        var exists = _fileSystem.FileExists(filePath);
        
        if (exists)
        {
            switch (mode.ToLowerInvariant())
            {
                case "skip":
                    _logger.LogInformation($"Skipped (exists): {filePath}");
                    return;
                case "fail":
                    throw new InvalidOperationException($"File already exists: {filePath}");
                case "overwrite-if-changed":
                    var existingContent = await _fileSystem.ReadAllTextAsync(filePath);
                    if (existingContent == newContent)
                    {
                        _logger.LogInformation($"Skipped (unchanged): {filePath}");
                        return;
                    }
                    break;
            }
        }

        if (!_dryRun)
        {
            _fileSystem.EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);
            await _fileSystem.WriteAllTextAsync(filePath, newContent);
        }
        
        _logger.LogInformation($"File: {filePath} [{(exists ? "modified" : "created")}]");
    }
}