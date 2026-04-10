using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Spectre;
using DotnetCleanup.Testing.IO;
using Spectre.Console;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace DotnetCleanup.IntegrationTests;

public sealed class CleanupCommandTests
{
    public static readonly string RootPath = InMemoryFileSystem.DefaultRootPath;
    public static readonly string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void Run_UsingLongOptionNames_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "--yes", "--what-if", "--no-move"]);
        var settings = result.Settings as CleanupSettings;

        // Assert
        Assert.True(settings?.StartedAt > default(DateTimeOffset));
        Assert.Equal(InMemoryFileSystem.DefaultRootPath, settings?.Path);
        Assert.True(settings?.SkipConfirm);
        Assert.True(settings?.Noop);
        Assert.False(settings?.SkipDelete);
        Assert.True(settings?.SkipMove);
        Assert.Equal(InMemoryFileSystem.DefaultTempPath, settings?.TempPath);
    }

    [Fact]
    public void Run_UsingSecondOptionNames_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "--yes", "--whatif"]);
        var settings = result.Settings as CleanupSettings;

        // Assert
        Assert.True(settings?.SkipConfirm);
        Assert.True(settings?.Noop);
        Assert.False(settings?.SkipDelete);
    }

    [Fact]
    public void Run_UsingNoDeleteOption_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "--yes", "--no-delete"]);
        var settings = result.Settings as CleanupSettings;

        // Assert
        Assert.True(settings?.SkipConfirm);
        Assert.False(settings?.Noop);
        Assert.True(settings?.SkipDelete);
    }

    [Fact]
    public void Run_UsingShortOptionNames_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "-y", "--noop"]);
        var settings = result.Settings as CleanupSettings;

        // Assert
        Assert.True(settings?.SkipConfirm);
        Assert.True(settings?.Noop);
        Assert.False(settings?.SkipDelete);
    }

    [Fact]
    public void Run_NonExistingRootPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [TempPath]));

        // Act
        var exception = Assert.Throws<DirectoryNotFoundException>(() => appTester.Run([RootPath, "--temp-path", TempPath,
            "-y", "--noop"]));

        // Assert
        Assert.Equal($"The given path does not exist: {RootPath}", exception.Message);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithMoveEnabled_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [RootPath]));

        // Act
        var exception = Assert.Throws<DirectoryNotFoundException>(() => appTester.Run([RootPath, "--temp-path", TempPath, "-y"]));

        // Assert
        Assert.Equal($"The given temporary path does not exist: {TempPath}", exception.Message);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithNoop_DoesNotThrow()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [RootPath]));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.True(settings.Noop);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithNoMove_DoesNotThrow()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [RootPath]));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-y", "--no-move"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.True(settings.SkipMove);
    }

    [Fact]
    public void Run_WithNoopAndAdditionalSkipFlags_StillBehavesAsNoop()
    {
        // Arrange
        var binPath = Root("projectA", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                binPath
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-y", "--noop", "--no-move", "--no-delete"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.True(settings.Noop);
        Assert.True(settings.SkipMove);
        Assert.True(settings.SkipDelete);
        Assert.Contains("Skipping moving paths", result.Output, StringComparison.Ordinal);
        Assert.Contains("Skipping deleting paths", result.Output, StringComparison.Ordinal);
        Assert.Contains(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_UsingLongIncludeOptionMultipleTimes_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "--include", "**/bin", "--include", "**/obj", "--include", "**/node_modules", "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(["**/bin", "**/obj", "**/node_modules"], settings.Include);
    }

    [Fact]
    public void Run_UsingLongExcludeOptionMultipleTimes_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "--exclude", "**/bin", "--exclude", "**/obj", "--exclude", "**/node_modules", "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(["**/bin", "**/obj", "**/node_modules"], settings.Exclude);
    }

    [Fact]
    public void Run_UsingShortIncludeOptionMultipleTimes_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "-p", "**/bin", "-p", "**/obj", "-p", "**/node_modules", "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(["**/bin", "**/obj", "**/node_modules"], settings.Include);
    }

    [Fact]
    public void Run_UsingShortExcludeOptionMultipleTimes_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "-x", "**/bin", "-x", "**/obj", "-x", "**/node_modules", "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(["**/bin", "**/obj", "**/node_modules"], settings.Exclude);
    }

    [Fact]
    public void Run_WithYesOption_SkipsPrompting()
    {
        // Arrange
        var testConsole = new TestConsole();
        testConsole.Input.PushTextWithEnter("n");

        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "bin")
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(Console: testConsole, FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath,
                "-p", "**/bin",
                "--yes",
                "--noop"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Proceed with the cleanup?", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Cleanup canceled by user", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenConfirmationRejected_ShowsCanceledAndListSummary()
    {
        // Arrange
        var testConsole = new TestConsole();
        testConsole.Input.PushTextWithEnter("n");

        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "bin")
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(Console: testConsole, FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath,
                "-p", "**/bin"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Proceed with the cleanup?", result.Output, StringComparison.Ordinal);
        Assert.Contains("Cleanup canceled by user", result.Output, StringComparison.Ordinal);
        Assert.Contains("1 path found", result.Output, StringComparison.Ordinal);
        Assert.Contains("1 succeeded.", result.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("minimal", false, false, false, false, false)]
    [InlineData("normal", true, true, false, false, true)]
    [InlineData("detailed", true, true, true, true, true)]
    public void Run_DifferentVerbosityLevels_OutputExpectedConsoleContent(
        string verbosity,
        bool expectPathsFoundMessage,
        bool expectSkipMessages,
        bool expectListStartMessage,
        bool expectPathOutput,
        bool expectSummaryMessage)
    {
        // Arrange
        var binPath = Root("projectA", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                binPath
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath,
                "-p", "**/bin",
                "-y",
                "--noop",
                "--verbosity", verbosity
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Cleanup process completed.", result.Output, StringComparison.Ordinal);

        if (expectPathsFoundMessage)
        {
            Assert.Contains("1 path found", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("path found", result.Output, StringComparison.Ordinal);
        }

        if (expectSkipMessages)
        {
            Assert.Contains("Skipping moving paths", result.Output, StringComparison.Ordinal);
            Assert.Contains("Skipping deleting paths", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("Skipping moving paths", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("Skipping deleting paths", result.Output, StringComparison.Ordinal);
        }

        if (expectListStartMessage)
        {
            Assert.Contains("Listing paths...", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("Listing paths...", result.Output, StringComparison.Ordinal);
        }

        if (expectPathOutput)
        {
            Assert.Contains(binPath, result.Output, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.DoesNotContain(binPath, result.Output, StringComparison.OrdinalIgnoreCase);
        }

        if (expectSummaryMessage)
        {
            Assert.Contains("1 succeeded.", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("1 succeeded.", result.Output, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Run_WithNoDelete_SummarizesMoveStep()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "bin")
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath,
                "-p", "**/bin",
                "-y",
                "--no-delete"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("1 succeeded.", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("0 succeeded.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenDeleteFails_ShowsTheStagedPath()
    {
        // Arrange
        var innerFileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "bin")
            ]);
        var fileSystem = new DeleteFailsForMovedDirectoryFileSystem(innerFileSystem);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath,
                "-p", "**/bin",
                "-y"
            ]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);
        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Error deleting path:", result.Output, StringComparison.Ordinal);
        Assert.Contains(GetTempRunPrefix(settings), result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain($"Error deleting path: {Root("projectA", "bin")}", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WhenListingFails_DoesNotReportNoMatchingPathsFound()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem(directories: [RootPath, TempPath]);
        fileSystem.ListFileExceptions.Add(RootPath, new IOException("list failed"));
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-y", "--noop"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Listing completed with failures", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("No matching paths found", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenListedPathDisappearsBeforeMove_ShowsMoveError()
    {
        // Arrange
        var innerFileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "bin")
            ]);
        var fileSystem = new MoveSourceDisappearsFileSystem(innerFileSystem);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-p", "**/bin", "-y"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"Error moving path: {Root("projectA", "bin")}", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WhenStagedPathDisappearsBeforeDelete_ShowsDeleteError()
    {
        // Arrange
        var innerFileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                Root("projectA", "bin")
            ]);
        var fileSystem = new DeleteTargetDisappearsFileSystem(innerFileSystem);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-p", "**/bin", "-y"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Error deleting path:", result.Output, StringComparison.Ordinal);
        Assert.Contains(GetTempRunPrefix(settings), result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--noop", "--no-move")]
    [InlineData("--noop", "--no-delete")]
    [InlineData("--noop", "--no-move", "--no-delete")]
    public void Run_NoopWithRedundantSkipFlags_BehavesLikeNoopAlone(params string[] extraFlags)
    {
        // Arrange (#18 - normalize ineffective option combinations)
        var binPath = Root("projectA", "bin");
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA"),
                binPath
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        var args = new List<string>
        {
            RootPath,
            "--temp-path", TempPath,
            "-p", "**/bin",
            "-y"
        };
        args.AddRange(extraFlags);

        // Act
        var result = appTester.Run([.. args]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("1 path found", result.Output, StringComparison.Ordinal);
        Assert.Contains(binPath, fileSystem.Directories, StringComparer.OrdinalIgnoreCase);
    }

    private static string Root(params string[] segments) => TestPath.Root(segments);

    private static string GetTempRunPrefix(CleanupSettings settings) => CleanupTempPath.GetRunDirectoryPrefix(TempPath, settings.StartedAt);

    private record AppTesterConfig(
        TestConsole? Console = null,
        IFileSystem? FileSystem = null,
        string[]? Directories = null,
        string[]? Files = null);

    private static CommandAppTester CreateAppTester(AppTesterConfig? config = null)
    {
        var testConsole = config?.Console ?? new TestConsole();
        var registrar = new SimpleTypeRegistrar();

        var fileSystem = config?.FileSystem ?? new InMemoryFileSystem(config?.Directories, config?.Files);

        registrar.RegisterInstance(typeof(IFileSystem), fileSystem);
        registrar.RegisterInstance(typeof(IAnsiConsole), testConsole);

        var app = new CommandAppTester(
            registrar,
            new CommandAppTesterSettings
            {
                TrimConsoleOutput = true
            },
            testConsole);

        app.SetDefaultCommand<CleanupCommand>();
        app.Configure(CommandAppCleanupCommand.Configurator);

        return app;
    }

    private sealed class DeleteFailsForMovedDirectoryFileSystem(InMemoryFileSystem innerFileSystem) : IFileSystem
    {
        private readonly InMemoryFileSystem _innerFileSystem = innerFileSystem ?? throw new ArgumentNullException(nameof(innerFileSystem));

        public void CreateDirectory(string path) => _innerFileSystem.CreateDirectory(path);

        public void DeleteDirectory(string path) => _innerFileSystem.DeleteDirectory(path);

        public void DeleteFile(string path) => _innerFileSystem.DeleteFile(path);

        public bool DirectoryExists(string path) => _innerFileSystem.DirectoryExists(path);

        public IEnumerable<string> EnumerateDirectories(string path) => _innerFileSystem.EnumerateDirectories(path);

        public IEnumerable<string> EnumerateFiles(string path) => _innerFileSystem.EnumerateFiles(path);

        public string GetCurrentDirectory() => _innerFileSystem.GetCurrentDirectory();

        public string GetTempPath() => _innerFileSystem.GetTempPath();

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            _innerFileSystem.MoveDirectory(sourcePath, destinationPath);
            _innerFileSystem.DeleteDirectoryExceptions.TryAdd(destinationPath, new IOException("delete failed"));
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            _innerFileSystem.MoveFile(sourcePath, destinationPath);
        }
    }

    private sealed class MoveSourceDisappearsFileSystem(InMemoryFileSystem innerFileSystem) : IFileSystem
    {
        private readonly InMemoryFileSystem _innerFileSystem = innerFileSystem ?? throw new ArgumentNullException(nameof(innerFileSystem));

        public void CreateDirectory(string path) => _innerFileSystem.CreateDirectory(path);

        public void DeleteDirectory(string path) => _innerFileSystem.DeleteDirectory(path);

        public void DeleteFile(string path) => _innerFileSystem.DeleteFile(path);

        public bool DirectoryExists(string path) => _innerFileSystem.DirectoryExists(path);

        public IEnumerable<string> EnumerateDirectories(string path) => _innerFileSystem.EnumerateDirectories(path);

        public IEnumerable<string> EnumerateFiles(string path) => _innerFileSystem.EnumerateFiles(path);

        public string GetCurrentDirectory() => _innerFileSystem.GetCurrentDirectory();

        public string GetTempPath() => _innerFileSystem.GetTempPath();

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            _innerFileSystem.DeleteDirectory(sourcePath);
            _innerFileSystem.MoveDirectory(sourcePath, destinationPath);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            _innerFileSystem.DeleteFile(sourcePath);
            _innerFileSystem.MoveFile(sourcePath, destinationPath);
        }
    }

    private sealed class DeleteTargetDisappearsFileSystem(InMemoryFileSystem innerFileSystem) : IFileSystem
    {
        private readonly InMemoryFileSystem _innerFileSystem = innerFileSystem ?? throw new ArgumentNullException(nameof(innerFileSystem));

        public void CreateDirectory(string path) => _innerFileSystem.CreateDirectory(path);

        public void DeleteDirectory(string path) => _innerFileSystem.DeleteDirectory(path);

        public void DeleteFile(string path) => _innerFileSystem.DeleteFile(path);

        public bool DirectoryExists(string path) => _innerFileSystem.DirectoryExists(path);

        public IEnumerable<string> EnumerateDirectories(string path) => _innerFileSystem.EnumerateDirectories(path);

        public IEnumerable<string> EnumerateFiles(string path) => _innerFileSystem.EnumerateFiles(path);

        public string GetCurrentDirectory() => _innerFileSystem.GetCurrentDirectory();

        public string GetTempPath() => _innerFileSystem.GetTempPath();

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            _innerFileSystem.MoveDirectory(sourcePath, destinationPath);
            _innerFileSystem.DeleteDirectory(destinationPath);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            _innerFileSystem.MoveFile(sourcePath, destinationPath);
            _innerFileSystem.DeleteFile(destinationPath);
        }
    }
}
