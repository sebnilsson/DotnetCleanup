using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DotnetCleanup;

public sealed class CleanupService
{
    private static readonly string[] DefaultIncludePatterns =
    [
        "bin",
        "obj",
        "node_modules"
    ];

    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(
        IAnsiConsole console,
        IFileSystem fileSystem,
        ILogger<CleanupService> logger)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> RunAsync(
        CleanupSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = BuildOptions(settings);

            var cleanupPaths = ResolveCleanupPaths(options);
            var topLevelPaths = FilterTopLevelPaths(cleanupPaths);

            if (topLevelPaths.Count == 0)
            {
                WriteLine("No matching paths were found.");
                return 0;
            }

            if (options.Verbosity >= VerbosityLevel.Normal)
            {
                WritePathList(topLevelPaths);
            }

            if (!ConfirmCleanup(options))
            {
                WriteLine("Cleanup cancelled.");
                return 0;
            }

            var result = await ExecuteCleanup(
                topLevelPaths,
                options,
                cancellationToken);

            WriteSummary(result, options);

            return result.ErrorCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup failed.");
            WriteError($"Cleanup failed: {ex.Message}");
            return 1;
        }
    }

    private CleanupOptions BuildOptions(CleanupSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var rootPath = string.IsNullOrWhiteSpace(settings.Path)
            ? _fileSystem.GetCurrentDirectory()
            : settings.Path;

        if (_fileSystem.FileExists(rootPath))
        {
            throw new InvalidOperationException(
                $"Path '{rootPath}' must be a directory.");
        }

        if (!_fileSystem.DirectoryExists(rootPath))
        {
            throw new DirectoryNotFoundException(
                $"No directory found at path '{rootPath}'.");
        }

        var includePatterns = settings.Paths.Length > 0
            ? settings.Paths
            : DefaultIncludePatterns;

        var cleanedIncludes = includePatterns
            .Select(NormalizePattern)
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .ToList();

        if (cleanedIncludes.Count == 0)
        {
            cleanedIncludes.AddRange(DefaultIncludePatterns);
        }

        if (includePatterns.Any(IsRootedPattern))
        {
            throw new ArgumentException("Include patterns must be relative paths.");
        }

        var cleanedExcludes = settings.Exclude
            .Select(NormalizePattern)
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .ToList();

        if (settings.Exclude.Any(IsRootedPattern))
        {
            throw new ArgumentException("Exclude patterns must be relative paths.");
        }

        var tempPath = string.IsNullOrWhiteSpace(settings.TempPath)
            ? Path.GetTempPath()
            : settings.TempPath;

        if (!settings.NoMove && !_fileSystem.DirectoryExists(tempPath))
        {
            throw new DirectoryNotFoundException(
                $"No temp-directory found at path '{tempPath}'.");
        }

        return new CleanupOptions(
            rootPath,
            cleanedIncludes,
            cleanedExcludes,
            tempPath,
            settings.ConfirmCleanup,
            settings.NoDelete,
            settings.NoMove,
            settings.Verbosity,
            DateTimeOffset.UtcNow);
    }

    private IReadOnlyList<string> ResolveCleanupPaths(CleanupOptions options)
    {
        var comparer = GetPathComparer();
        var cleanupPaths = new HashSet<string>(comparer);

        var rootDirectory = Path.GetFullPath(options.RootPath);
        if (!_fileSystem.DirectoryExists(rootDirectory))
        {
            return Array.Empty<string>();
        }

        var matches = GetMatches(
            rootDirectory,
            options.IncludePatterns,
            options.ExcludePatterns);

        foreach (var match in matches)
        {
            var fullPath = Path.GetFullPath(match);
            if (Path.IsPathFullyQualified(fullPath))
            {
                cleanupPaths.Add(fullPath);
            }
        }

        return cleanupPaths
            .Where(PathExists)
            .OrderByDescending(path => _fileSystem.DirectoryExists(path))
            .ThenBy(path => path, comparer)
            .ToList();
    }

    private IReadOnlyList<string> GetMatches(
        string rootDirectory,
        IReadOnlyList<string> includePatterns,
        IReadOnlyList<string> excludePatterns)
    {
        var cleanedIncludes = includePatterns
            .Select(NormalizePattern)
            .Select(ExpandImplicitRecursivePattern)
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .ToList();

        var cleanedExcludes = excludePatterns
            .Select(NormalizePattern)
            .Select(ExpandImplicitRecursivePattern)
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .ToList();

        if (cleanedIncludes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var includeMatchers = cleanedIncludes
            .Select(pattern => new GlobMatcher(pattern, IsCaseInsensitive()))
            .ToList();

        var excludeMatchers = cleanedExcludes
            .Select(pattern => new GlobMatcher(pattern, IsCaseInsensitive()))
            .ToList();

        var results = new List<string>();

        foreach (var entry in _fileSystem.EnumerateFileSystemEntries(rootDirectory, recursive: true))
        {
            var relative = NormalizeRelativePath(rootDirectory, entry);
            if (!includeMatchers.Any(matcher => matcher.IsMatch(relative)))
            {
                continue;
            }

            if (excludeMatchers.Any(matcher => matcher.IsMatch(relative)))
            {
                continue;
            }

            results.Add(entry);
        }

        return results;
    }

    private static IReadOnlyList<string> FilterTopLevelPaths(
        IReadOnlyList<string> paths)
    {
        var comparison = GetPathComparison();
        var normalizedPaths = paths
            .Select(path => new PathEntry(path, NormalizeForComparison(path)))
            .OrderBy(entry => entry.ComparisonPath.Length)
            .ThenBy(entry => entry.ComparisonPath, GetPathComparer())
            .ToList();

        var results = new List<string>();

        foreach (var entry in normalizedPaths)
        {
            var hasParent = results.Any(existing =>
                IsNestedPath(entry.ComparisonPath, NormalizeForComparison(existing), comparison));

            if (!hasParent)
            {
                results.Add(entry.Path);
            }
        }

        return results;
    }

    private bool ConfirmCleanup(CleanupOptions options)
    {
        if (options.ConfirmCleanup)
        {
            WriteDebug(options, "Cleanup automatically confirmed by command-option.");
            return true;
        }

        WriteLine(string.Empty);

        var confirmed = _console.Confirm(
            "Do you want to clean up these paths?",
            defaultValue: false);

        WriteLine(string.Empty);

        if (confirmed)
        {
            WriteDebug(options, "Cleanup confirmed.");
        }
        else
        {
            WriteDebug(options, "Cleanup cancelled.");
        }

        return confirmed;
    }

    private async Task<CleanupResult> ExecuteCleanup(
        IReadOnlyList<string> cleanupPaths,
        CleanupOptions options,
        CancellationToken cancellationToken)
    {
        var result = new CleanupResult();
        var moveRoots = new HashSet<string>(GetPathComparer());

        foreach (var cleanupPath in cleanupPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var targetPath = cleanupPath;
                if (!options.NoMove)
                {
                    targetPath = MovePath(cleanupPath, options, out var moveRoot);
                    if (!string.IsNullOrWhiteSpace(moveRoot))
                    {
                        moveRoots.Add(moveRoot);
                    }

                    if (options.Verbosity >= VerbosityLevel.Detailed)
                    {
                        WriteLine($"Moved: {cleanupPath} -> {targetPath}");
                    }
                }

                if (!options.NoDelete)
                {
                    DeletePath(targetPath);

                    if (options.Verbosity >= VerbosityLevel.Detailed)
                    {
                        WriteLine($"Deleted: {targetPath}");
                    }
                }

                result.AddSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup failed for {Path}.", cleanupPath);
                result.AddError();
                WriteError($"Failed: {cleanupPath} ({ex.Message})");
            }
        }

        if (!options.NoDelete && !options.NoMove)
        {
            foreach (var moveRoot in moveRoots)
            {
                TryDeleteEmptyDirectory(moveRoot);
            }
        }

        await Task.CompletedTask;
        return result;
    }

    private string MovePath(
        string cleanupPath,
        CleanupOptions options,
        out string? moveRoot)
    {
        moveRoot = GetMoveRoot(cleanupPath, options);
        EnsureDirectory(moveRoot);

        var name = Path.GetFileName(
            cleanupPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetRandomFileName();
        }

        var destinationPath = GetUniqueDestinationPath(moveRoot, name);

        if (IsFile(cleanupPath))
        {
            _fileSystem.MoveFile(cleanupPath, destinationPath);
        }
        else
        {
            _fileSystem.MoveDirectory(cleanupPath, destinationPath);
        }

        return destinationPath;
    }

    private string GetMoveRoot(string cleanupPath, CleanupOptions options)
    {
        var cleanupFolder = $"~dotnetcleanup-{options.StartedAt:yyyyMMdd-HHmmss}";
        var moveRoot = Path.Combine(options.TempPath, cleanupFolder);

        var moveRootDrive = Path.GetPathRoot(moveRoot);
        var cleanupDrive = Path.GetPathRoot(cleanupPath);

        if (!string.IsNullOrWhiteSpace(moveRootDrive)
            && !string.IsNullOrWhiteSpace(cleanupDrive)
            && string.Equals(moveRootDrive, cleanupDrive, GetPathComparison()))
        {
            return moveRoot;
        }

        var parent = Path.GetDirectoryName(cleanupPath);
        return string.IsNullOrWhiteSpace(parent)
            ? moveRoot
            : Path.Combine(parent, cleanupFolder);
    }

    private string GetUniqueDestinationPath(string moveRoot, string name)
    {
        var destinationPath = Path.Combine(moveRoot, name);
        if (!PathExists(destinationPath))
        {
            return destinationPath;
        }

        var uniqueName = $"{name}-{Path.GetRandomFileName()}";
        return Path.Combine(moveRoot, uniqueName);
    }

    private void DeletePath(string cleanupPath)
    {
        if (IsFile(cleanupPath))
        {
            _fileSystem.DeleteFile(cleanupPath);
        }
        else
        {
            _fileSystem.DeleteDirectory(cleanupPath, recursive: true);
        }
    }

    private void EnsureDirectory(string path)
    {
        if (_fileSystem.DirectoryExists(path))
        {
            return;
        }

        _fileSystem.CreateDirectory(path);
    }

    private void TryDeleteEmptyDirectory(string path)
    {
        if (!_fileSystem.DirectoryExists(path))
        {
            return;
        }

        var hasEntries = _fileSystem.EnumerateFileSystemEntries(path, recursive: false).Any();
        if (hasEntries)
        {
            return;
        }

        _fileSystem.DeleteDirectory(path, recursive: false);
    }

    private bool PathExists(string path) =>
        _fileSystem.DirectoryExists(path) || _fileSystem.FileExists(path);

    private bool IsFile(string path) => _fileSystem.FileExists(path);

    private void WritePathList(IReadOnlyList<string> paths)
    {
        WriteLine("Paths to clean:");
        foreach (var path in paths)
        {
            WriteLine($"- {path}");
        }
        WriteLine(string.Empty);
    }

    private void WriteSummary(CleanupResult result, CleanupOptions options)
    {
        if (options.NoMove && options.NoDelete)
        {
            WriteLine("No changes were made because both --no-move and --no-delete are set.");
            return;
        }

        if (options.NoDelete)
        {
            WriteLine($"Moved {result.SuccessCount} paths. Deletion is disabled.");
            if (result.ErrorCount > 0)
            {
                WriteLine($"Encountered {result.ErrorCount} errors.");
            }

            return;
        }

        if (options.NoMove)
        {
            WriteLine($"Deleted {result.SuccessCount} paths with {result.ErrorCount} errors.");
            return;
        }

        WriteLine($"Cleaned {result.SuccessCount} paths with {result.ErrorCount} errors.");
    }

    private void WriteLine(string message)
    {
        _console.MarkupLine(Markup.Escape(message));
    }

    private void WriteError(string message)
    {
        _console.MarkupLine($"[red]{Markup.Escape(message)}[/]");
    }

    private void WriteDebug(CleanupOptions options, string message)
    {
        if (options.Verbosity < VerbosityLevel.Debug)
        {
            return;
        }

        _logger.LogDebug(message);
        WriteLine($"DEBUG: {message}");
    }

    private static bool IsRootedPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var trimmed = pattern.Trim()
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.IsPathRooted(trimmed);
    }

    private static string NormalizePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return string.Empty;
        }

        var cleaned = pattern.Trim()
            .Replace('\\', '/');

        while (cleaned.StartsWith("./", StringComparison.Ordinal))
        {
            cleaned = cleaned[2..];
        }

        cleaned = cleaned.Trim('/');

        if (cleaned == ".")
        {
            return string.Empty;
        }

        return cleaned;
    }

    private static string ExpandImplicitRecursivePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return string.Empty;
        }

        if (HasWildcards(pattern) || HasSeparator(pattern))
        {
            return pattern;
        }

        return $"**/{pattern}";
    }

    private static bool HasSeparator(string pattern) => pattern.Contains('/');

    private static string NormalizeRelativePath(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return NormalizePathForMatch(relative);
    }

    private static string NormalizePathForMatch(string path) =>
        path.Replace('\\', '/');

    private static string NormalizeForComparison(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);

        if (!string.IsNullOrWhiteSpace(root) && fullPath.Length > root.Length)
        {
            fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return fullPath;
    }

    private static bool IsNestedPath(
        string child,
        string parent,
        StringComparison comparison)
    {
        if (child.Length <= parent.Length)
        {
            return false;
        }

        if (!child.StartsWith(parent, comparison))
        {
            return false;
        }

        var boundary = child[parent.Length];
        return boundary == Path.DirectorySeparatorChar || boundary == Path.AltDirectorySeparatorChar;
    }

    private static bool HasWildcards(string pattern) =>
        pattern.Contains('*') || pattern.Contains('?');

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static StringComparison GetPathComparison() =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static bool IsCaseInsensitive() => OperatingSystem.IsWindows();

    private sealed record PathEntry(string Path, string ComparisonPath);
}
