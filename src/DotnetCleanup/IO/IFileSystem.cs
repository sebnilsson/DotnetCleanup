namespace DotnetCleanup.IO;

public interface IFileSystem
{
    void CreateDirectory(string path);
    void DeleteDirectory(string path);
    void DeleteFile(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> EnumerateDirectories(string path);
    IEnumerable<string> EnumerateFiles(string path);
    string GetCurrentDirectory();
    string GetTempPath();
    void MoveDirectory(string sourcePath, string destinationPath);
    void MoveFile(string sourcePath, string destinationPath);
}
