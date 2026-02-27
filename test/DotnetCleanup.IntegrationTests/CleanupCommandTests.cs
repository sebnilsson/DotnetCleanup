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
    public const string DefaultRootPath = InMemoryFileSystem.DefaultRootPath;
    public const string DefaultTempPath = InMemoryFileSystem.DefaultTempPath;

    [Fact]
    public void Run_UsingLongOptionNames_SetsSettings()
    {
        // Arrange
        var appTester = CreateAppTester();

        // Act
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [DefaultTempPath]));

        // Act
        var exception = Assert.Throws<DirectoryNotFoundException>(() => appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
            "-y", "--noop"]));

        // Assert
        Assert.Equal($"The given path does not exist: {DefaultRootPath}", exception.Message);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithMoveEnabled_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [DefaultRootPath]));

        // Act
        var exception = Assert.Throws<DirectoryNotFoundException>(() => appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath, "-y"]));

        // Assert
        Assert.Equal($"The given temporary path does not exist: {DefaultTempPath}", exception.Message);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithNoop_DoesNotThrow()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [DefaultRootPath]));

        // Act
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath, "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.True(settings.Noop);
    }

    [Fact]
    public void Run_NonExistingTempPath_WithNoMove_DoesNotThrow()
    {
        // Arrange
        var appTester = CreateAppTester(new AppTesterConfig(Directories: [DefaultRootPath]));

        // Act
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath, "-y", "--no-move"]);
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
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
        var result = appTester.Run([DefaultRootPath, "--temp-path", DefaultTempPath,
            "-x", "**/bin", "-x", "**/obj", "-x", "**/node_modules", "-y", "--noop"]);
        var settings = Assert.IsType<CleanupSettings>(result.Settings);

        // Assert
        Assert.Equal(["**/bin", "**/obj", "**/node_modules"], settings.Exclude);
    }

    private record AppTesterConfig(
        TestConsole? Console = null,
        string[]? Directories = null,
        string[]? Files = null);

    private static CommandAppTester CreateAppTester(AppTesterConfig? config = null)
    {
        var testConsole = config?.Console ?? new TestConsole();
        var registrar = new SimpleTypeRegistrar();

        var directories = config?.Directories;

        registrar.RegisterInstance(typeof(IFileSystem), new InMemoryFileSystem(directories, config?.Files));
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
