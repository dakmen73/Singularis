public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    string GetCurrentDirectory();
    string CombinePaths(params string[] paths);
    string GetDirectoryName(string path);
}

public class PhysicalFileSystem : IFileSystem { /* implementace */ }