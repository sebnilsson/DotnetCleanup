namespace DotnetCleanup;

public interface IFileSystem
{
    string GetCurrentDirectory();
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> EnumerateFileSystemEntries(string path, bool recursive);
    void CreateDirectory(string path);
    void MoveFile(string sourcePath, string destinationPath);
    void MoveDirectory(string sourcePath, string destinationPath);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive);
}

internal sealed class PhysicalFileSystem : IFileSystem
{
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateFileSystemEntries(string path, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFileSystemEntries(path, "*", option);
    }

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void MoveFile(string sourcePath, string destinationPath) =>
        File.Move(sourcePath, destinationPath);

    public void MoveDirectory(string sourcePath, string destinationPath) =>
        Directory.Move(sourcePath, destinationPath);

    public void DeleteFile(string path) => File.Delete(path);

    public void DeleteDirectory(string path, bool recursive) =>
        Directory.Delete(path, recursive);
}
