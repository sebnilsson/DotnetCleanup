using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Spectre;
using DotNetCleanup.Testing.IO;
using Spectre.Console;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace DotnetCleanup.IntegrationTests;

public sealed class CleanupCommandTests
{
    public const string RootPath = InMemoryFileSystem.DefaultRootPath;
    public const string TempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void Run_UsingLongOptionNames_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([RootPath, "--temp-path", TempPath,
            "--confirm", "--what-if", "--no-move"]);
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
    public void Run_WithConfirmOption_SkipsPrompting()
    {
        // Arrange
        var testConsole = new TestConsole();
        testConsole.Input.PushTextWithEnter("n");

        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                $@"{RootPath}\projectA",
                $@"{RootPath}\projectA\bin"
            ]);
        var appTester = CreateAppTester(new AppTesterConfig(Console: testConsole, FileSystem: fileSystem));

        // Act
        var result = appTester.Run(
            [
                RootPath,
                "--temp-path", TempPath,
                "-p", "**/bin",
                "--confirm",
                "--noop"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Proceed with the cleanup?", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Cleanup canceled by user", result.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("minimal", false, false, false, false, false)]
    [InlineData("normal", true, true, false, false, true)]
    [InlineData("detailed", true, true, true, true, true)]
    public void Run_DifferentVerbosityLevels_OutputExpectedConsoleContent(
        string verbosity,
        bool expectFilesFoundMessage,
        bool expectSkipMessages,
        bool expectListStartMessage,
        bool expectPathOutput,
        bool expectSummaryMessage)
    {
        // Arrange
        var binPath = $@"{RootPath}\projectA\bin";
        var fileSystem = new InMemoryFileSystem(
            directories:
            [
                RootPath,
                TempPath,
                $@"{RootPath}\projectA",
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

        if (expectFilesFoundMessage)
        {
            Assert.Contains("1 files found", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("files found", result.Output, StringComparison.Ordinal);
        }

        if (expectSkipMessages)
        {
            Assert.Contains("Skipping moving files", result.Output, StringComparison.Ordinal);
            Assert.Contains("Skipping deleting files", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("Skipping moving files", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("Skipping deleting files", result.Output, StringComparison.Ordinal);
        }

        if (expectListStartMessage)
        {
            Assert.Contains("Listing files...", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("Listing files...", result.Output, StringComparison.Ordinal);
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
            Assert.Contains("0 succeeded.", result.Output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("0 succeeded.", result.Output, StringComparison.Ordinal);
        }
    }

    private record AppTesterConfig(
        TestConsole? Console = null,
        InMemoryFileSystem? FileSystem = null,
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
}
