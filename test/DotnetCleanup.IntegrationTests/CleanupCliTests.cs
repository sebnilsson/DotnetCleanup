using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Spectre;
using DotNetCleanup.Testing.IO;
using Spectre.Console;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace DotnetCleanup.IntegrationTests;

public sealed class CleanupCliTests
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
        Assert.True(settings?.SkipDelete);
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
        Assert.True(settings?.SkipDelete);
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
