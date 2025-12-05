public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string command, string workingDirectory, TimeSpan? timeout = null);
    bool IsAllowed(string command);
}

public class WhitelistCommandRunner : ICommandRunner
{
    private readonly HashSet<string> _allowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "dotnet", "git"
    };
    
    public bool IsAllowed(string command) 
        => _allowedCommands.Contains(command.Split(' ')[0]);
    
    public async Task<CommandResult> RunAsync(string command, string workingDirectory, TimeSpan? timeout = null)
    {
        if (!IsAllowed(command))
            throw new SecurityException($"Command not allowed: {command}");
            
        // Implementace spouštění procesu
    }
}