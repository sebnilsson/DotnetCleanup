using System;
using System.Collections.Generic;
using System.Linq;
using DotnetCleanup.IO;

namespace DotNetCleanup.Tests.IO;

public class InMemoryFileSystem : IFileSystem
{
    public HashSet<string> Directories { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Files { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public void CreateDirectory(string path)
    {
        Directories.Add(path);
    }

    public void DeleteDirectory(string path)
    {
        Directories.Remove(path);

        Files.RemoveWhere(x => x.StartsWith(path));
    }

    public void DeleteFile(string path)
    {
        Files.Remove(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directories.Contains(path);
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        return Files.Where(x => x.StartsWith(path) && !x.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        return Directories.Where(x => x.StartsWith(path) && !x.Equals(path, StringComparison.OrdinalIgnoreCase));
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
        if (Directories.Remove(sourcePath))
        {
            Directories.Add(destinationPath);
        }
    }

    public void MoveFile(string sourcePath, string destinationPath)
    {
        if (Files.Remove(sourcePath))
        {
            Files.Add(destinationPath);
        }
    }
}
