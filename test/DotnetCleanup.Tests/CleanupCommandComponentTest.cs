using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Spectre;
using DotnetCleanup.Tests.IO;
using Spectre.Console;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace DotnetCleanup.Tests;

public sealed class CleanupCommandComponentTest
{
    public static readonly string RootPath = InMemoryFileSystem.DefaultRootPath;
    public static readonly string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Theory]
    [InlineData(true, true, false, true, "--yes", "--what-if", "--no-move")]
    [InlineData(true, true, false, false, "--yes", "--whatif")]
    [InlineData(true, false, true, false, "--yes", "--no-delete")]
    [InlineData(true, true, false, false, "-y", "--noop")]
    public void Run_UsingOptionAliases_SetsSettings(
        bool skipConfirm,
        bool noop,
        bool skipDelete,
        bool skipMove,
        params string[] args)
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, .. args]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.True(settings.StartedAt > default(DateTimeOffset));
        Assert.Equal(InMemoryFileSystem.DefaultRootPath, settings.Path);
        Assert.Equal(skipConfirm, settings.SkipConfirm);
        Assert.Equal(noop, settings.Noop);
        Assert.Equal(skipDelete, settings.SkipDelete);
        Assert.Equal(skipMove, settings.SkipMove);
        Assert.Equal(InMemoryFileSystem.DefaultTempPath, settings.TempPath);
    }

    [Fact]
    public void Run_NonExistingRootPath_ReturnsErrorResult()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [TempPath]));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-y", "--noop"]);

        // Assert
        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithMoveEnabled_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [RootPath], PropagateExceptions: true));

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
        Assert.Contains(binPath, fileSystem.Directories, TestPath.PathComparer);
    }

    [Theory]
    [InlineData("--include")]
    [InlineData("-p")]
    public void Run_UsingIncludeOptionMultipleTimes_SetsSettings(string option)
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            option, "**/bin", option, "**/obj", option, "**/node_modules", "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(["**/bin", "**/obj", "**/node_modules"], settings.Include);
    }

    [Theory]
    [InlineData("--exclude")]
    [InlineData("-x")]
    public void Run_UsingExcludeOptionMultipleTimes_SetsSettings(string option)
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            option, "**/bin", option, "**/obj", option, "**/node_modules", "-y", "--noop"]);
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
                "--no-move"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain(Root("projectA", "bin"), fileSystem.Directories, TestPath.PathComparer);
        Assert.DoesNotContain("Proceed with the cleanup?", result.Output, StringComparison.Ordinal);
        Assert.Contains("Cleanup process completed", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenConfirmationRejected_LeavesMatchedPaths()
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
                Root("projectA", "bin"),
                Root("projectA", "obj")
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(Console: testConsole, FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(Root("projectA", "bin"), fileSystem.Directories, TestPath.PathComparer);
        Assert.Contains(Root("projectA", "obj"), fileSystem.Directories, TestPath.PathComparer);
        Assert.Contains("Cleanup canceled by user", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WithDetailedNoopAndNoMatches_WritesNoMatchingPathsFound()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                Root("projectA")
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
                "--verbosity", "detailed"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Finding paths", result.Output, StringComparison.Ordinal);
        Assert.Contains("No matching paths found", result.Output, StringComparison.Ordinal);
        Assert.Contains("Cleanup process completed", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WithNoDelete_MovesWithoutDeletingStagedPath()
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
        Assert.DoesNotContain(Root("projectA", "bin"), fileSystem.Directories, TestPath.PathComparer);
        Assert.Contains(fileSystem.Directories, path => path.EndsWith(TestPath.Combine("projectA", "bin"), TestPath.PathComparison));
    }

    [Fact]
    public void Run_WhenDeleteCompletes_RemovesTempCatalog()
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
                "--verbosity", "detailed"
            ]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain(fileSystem.Directories, path => path.StartsWith(GetTempRunPrefix(settings), TestPath.PathComparison));
    }

    [Fact]
    public void Run_WhenDeleteFails_LeavesStagedPath()
    {
        // Arrange
        var fileSystem = new DeleteFailsForMovedDirectoryFileSystem(
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
                "-y"
            ]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(fileSystem.Directories, path => path.StartsWith(GetTempRunPrefix(settings), TestPath.PathComparison));
    }

    [Fact]
    public void Run_WhenListedPathDisappearsBeforeMove_LeavesOriginalPath()
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
        fileSystem.MoveDirectoryExceptions.Add(binPath, new DirectoryNotFoundException("source vanished"));
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-p", "**/bin", "-y"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(binPath, fileSystem.Directories, TestPath.PathComparer);
        Assert.Contains("Error moving path", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenMoveFails_LeavesOriginalPath()
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
        fileSystem.MoveDirectoryExceptions.Add(binPath, new IOException("move failed"));
        var appTester = CreateAppTester(new AppTesterConfig(FileSystem: fileSystem));

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath, "-p", "**/bin", "-y"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(binPath, fileSystem.Directories, TestPath.PathComparer);
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
        Assert.Contains(binPath, fileSystem.Directories, TestPath.PathComparer);
    }

    private static string Root(params string[] segments) => TestPath.Root(segments);

    private static string GetTempRunPrefix(CleanupSettings settings) => CleanupTempPath.GetRunDirectoryPrefix(TempPath, settings.StartedAt);

    private static string GetTempRunPath(string sourcePath, string destinationPath)
    {
        var relativePath = PathUtility.GetRelativePath(RootPath, sourcePath)
            ?? throw new ArgumentException($"Failed to resolve relative path for {sourcePath}", nameof(sourcePath));

        return destinationPath[..^(relativePath.Length + 1)];
    }

    private record AppTesterConfig(
        TestConsole? Console = null,
        IFileSystem? FileSystem = null,
        string[]? Directories = null,
        string[]? Files = null,
        bool PropagateExceptions = false);

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
        app.Configure(configurator =>
        {
            CommandAppCleanupCommand.Configurator(configurator);

            if (config?.PropagateExceptions == true)
            {
                configurator.Settings.PropagateExceptions = true;
            }
        });

        return app;
    }

    private sealed class DeleteFailsForMovedDirectoryFileSystem(string[] directories) : InMemoryFileSystem(directories)
    {
        public override void MoveDirectory(string sourcePath, string destinationPath)
        {
            base.MoveDirectory(sourcePath, destinationPath);
            DeleteDirectoryExceptions.TryAdd(GetTempRunPath(sourcePath, destinationPath), new IOException("delete failed"));
        }

        public override void MoveFile(string sourcePath, string destinationPath)
        {
            base.MoveFile(sourcePath, destinationPath);
            DeleteDirectoryExceptions.TryAdd(GetTempRunPath(sourcePath, destinationPath), new IOException("delete failed"));
        }
    }
}
