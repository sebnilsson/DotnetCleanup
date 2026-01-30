using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Spectre.Console.Testing;
using Xunit;

namespace DotnetCleanup.IntegrationTests;

public sealed class CleanupCliTests
{
    [Fact]
    public void HelpCommandShowsUsage()
    {
        var console = new TestConsole();
        var result = Run(console, ["--help"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cleanup", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfirmCleanupFlagSkipsPrompt()
    {
        using var workspace = new TempWorkspace();
        var binPath = Path.Combine(workspace.RootPath, "bin");
        Directory.CreateDirectory(binPath);

        var console = new TestConsole();
        var result = Run(console, [
            "-y",
            "--no-delete",
            "--no-move",
            "--paths",
            "**/bin",
            workspace.RootPath
        ]);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Do you want to clean up", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PromptAppearsWhenConfirmNotProvided()
    {
        using var workspace = new TempWorkspace();
        var binPath = Path.Combine(workspace.RootPath, "bin");
        Directory.CreateDirectory(binPath);

        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("n");
        var result = Run(console, [
            "--paths",
            "**/bin",
            workspace.RootPath
        ]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Do you want to clean up", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cleanup cancelled", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidPathReturnsFailureExitCode()
    {
        var console = new TestConsole();

        var missingPath = Path.Combine(
            Path.GetTempPath(),
            "dotnetcleanup-tests",
            Guid.NewGuid().ToString("N"));

        var result = Run(console, [
            "-y",
            "--paths",
            "**/bin",
            missingPath
        ]);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("No directory found", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MovesAndDeletesByDefault()
    {
        using var workspace = new TempWorkspace();
        var binPath = Path.Combine(workspace.RootPath, "bin");
        Directory.CreateDirectory(binPath);
        File.WriteAllText(Path.Combine(binPath, "artifact.txt"), "data");

        var console = new TestConsole();
        var result = Run(console, [
            "-y",
            "--paths",
            "**/bin",
            "--temp-path",
            workspace.TempPath,
            workspace.RootPath
        ]);

        Assert.Equal(0, result.ExitCode);
        Assert.False(Directory.Exists(binPath));
        Assert.Empty(Directory.GetDirectories(workspace.TempPath, "~dotnetcleanup-*"));
    }

    private static AppRunResult Run(TestConsole console, string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<CleanupService>();

        var app = CleanupApp.Build(console, logger, new PhysicalFileSystem());
        var normalizedArgs = CleanupApp.NormalizeArgs(args);
        var exitCode = app.Run(normalizedArgs);

        return new AppRunResult(exitCode, console.Output);
    }

    private sealed record AppRunResult(int ExitCode, string Output);

    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            RootPath = Path.Combine(
                Path.GetTempPath(),
                "dotnetcleanup-tests",
                Guid.NewGuid().ToString("N"));
            TempPath = Path.Combine(RootPath, "temp");

            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(TempPath);
        }

        public string RootPath { get; }

        public string TempPath { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Directory.Delete(RootPath, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
