using DotnetCleanup.IO;

namespace DotNetCleanup.Tests.IO;

public class InMemoryFileSystem(string[]? directories = null, string[]? files = null) : IFileSystem
{
    public HashSet<string> Directories { get; } = new HashSet<string>(directories ?? [], StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Files { get; } = new HashSet<string>(files ?? [], StringComparer.OrdinalIgnoreCase);

    public Exception? DeleteDirectoryException { get; set; }

    public Exception? DeleteFileException { get; set; }

    public Exception? EnumerateDirectoriesException { get; set; }

    public Exception? EnumerateFilesException { get; set; }

    public Exception? MoveDirectoryException { get; set; }

    public Exception? MoveFileException { get; set; }

    public void CreateDirectory(string path)
    {
        Directories.Add(path);
    }

    public void DeleteDirectory(string path)
    {
        if (DeleteDirectoryException != null)
        {
            throw DeleteDirectoryException;
        }

        Directories.Remove(path);
        Files.RemoveWhere(x => x.StartsWith(path, StringComparison.OrdinalIgnoreCase));
    }

    public void DeleteFile(string path)
    {
        if (DeleteFileException != null)
        {
            throw DeleteFileException;
        }

        Files.Remove(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directories.Contains(path);
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        if (EnumerateFilesException != null)
        {
            throw EnumerateFilesException;
        }

        return Files.Where(x => x.StartsWith(path, StringComparison.OrdinalIgnoreCase) && !x.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        if (EnumerateDirectoriesException != null)
        {
            throw EnumerateDirectoriesException;
        }

        return Directories.Where(x => x.StartsWith(path, StringComparison.OrdinalIgnoreCase) && !x.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    //public bool FileExists(string path)
    //{
    //    return Files.Contains(path);
    //}

    public string GetCurrentDirectory()
    {
        return "C:\\InMemoryCurrentDirectory";
    }

    //public bool GetIsFile(string path)
    //{
    //    return Files.Contains(path);
    //}

    public string GetTempPath()
    {
        return "C:\\InMemoryTempPath";
    }

    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        if (MoveDirectoryException != null)
        {
            throw MoveDirectoryException;
        }

        if (Directories.Remove(sourcePath))
        {
            Directories.Add(destinationPath);
        }
    }

    public void MoveFile(string sourcePath, string destinationPath)
    {
        if (MoveFileException != null)
        {
            throw MoveFileException;
        }

        if (Files.Remove(sourcePath))
        {
            Files.Add(destinationPath);
        }
    }
}
