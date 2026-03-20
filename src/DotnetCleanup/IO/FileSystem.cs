namespace DotnetCleanup.IO;

internal sealed class FileSystem : IFileSystem
{
    private static readonly EnumerationOptions s_enumerationOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        ReturnSpecialDirectories = false,
        AttributesToSkip = 0
    };

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path) => Directory.Delete(path, recursive: true);

    public void DeleteFile(string path) => File.Delete(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateFiles(string rootDirectory)
    {
        return Directory.EnumerateFiles(rootDirectory, "*", s_enumerationOptions);
    }

    public IEnumerable<string> EnumerateDirectories(string rootDirectory)
    {
        return Directory.EnumerateDirectories(rootDirectory, "*", s_enumerationOptions);
    }

    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    public string GetTempPath() => Path.GetTempPath();

    public void MoveDirectory(string sourcePath, string destinationPath) =>
        Directory.Move(sourcePath, destinationPath);

    public void MoveFile(string sourcePath, string destinationPath) =>
        File.Move(sourcePath, destinationPath);
}
