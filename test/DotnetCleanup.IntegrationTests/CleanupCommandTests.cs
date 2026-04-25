using System.Diagnostics;
using DotnetCleanup;
using Xunit;

namespace DotnetCleanup.IntegrationTests;

public sealed class CleanupCommandTests
{
    [Fact]
    public async Task Run_WithYesAndNoop_LeavesMatchedDirectoryUntouched()
    {
        // Arrange
        using var workspace = new ProcessTestWorkspace();
        var binPath = workspace.CreateRootDirectory("projectA", "bin");

        // Act
        var result = await RunAppAsync(
            workspace.RootPath,
            [
                workspace.RootPath,
                "--temp-path", workspace.TempPath,
                "-p", "**/bin",
                "-y",
                "--noop"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Cleanup process completed.", result.Output, StringComparison.Ordinal);
        Assert.Contains("Found: 1 paths", result.Output, StringComparison.Ordinal);
        Assert.True(Directory.Exists(binPath));
        Assert.Empty(workspace.GetTempRunDirectories());
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task Run_WithoutPathArgument_UsesCurrentDirectoryAndLeavesStagedPathsWhenNoDelete()
    {
        // Arrange
        using var workspace = new ProcessTestWorkspace();
        var binPath = workspace.CreateRootDirectory("projectA", "bin");

        // Act
        var result = await RunAppAsync(
            workspace.RootPath,
            [
                "--temp-path", workspace.TempPath,
                "-p", "**/bin",
                "-y",
                "--no-delete"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var tempRunPath = Assert.Single(workspace.GetTempRunDirectories());

        Assert.DoesNotContain("Deleted:", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain(binPath, Directory.GetDirectories(workspace.RootPath, "*", SearchOption.AllDirectories), StringComparer.OrdinalIgnoreCase);
        Assert.True(Directory.Exists(Path.Combine(tempRunPath, "projectA", "bin")));
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task Run_WithBracketedPaths_EscapesConsoleOutputAndDeletesMatchedDirectory()
    {
        // Arrange
        using var workspace = new ProcessTestWorkspace();
        var binPath = workspace.CreateRootDirectory("project[1]", "bin");

        // Act
        var result = await RunAppAsync(
            workspace.RootPath,
            [
                workspace.RootPath,
                "--temp-path", workspace.TempPath,
                "-p", "**/bin",
                "-y",
                "--verbosity", "detailed"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(Path.Combine("project[1]", "bin"), result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Delete step completed.", result.Output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(binPath));
        Assert.Empty(workspace.GetTempRunDirectories());
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task Run_NonExistingRootPath_ReturnsErrorResult()
    {
        // Arrange
        using var workspace = new ProcessTestWorkspace();
        var missingPath = Path.Combine(workspace.RootPath, "missing-root");

        // Act
        var result = await RunAppAsync(
            workspace.RootPath,
            [
                missingPath,
                "--temp-path", workspace.TempPath,
                "-y",
                "--noop"
            ]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("The given path does not exist:", result.Output, StringComparison.Ordinal);
        Assert.Contains("missing-root", result.Output, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.Error);
    }

    private static string ApplicationPath => typeof(CleanupService).Assembly.Location;

    private static async Task<ProcessResult> RunAppAsync(string workingDirectory, string[] args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        process.StartInfo.EnvironmentVariables["NO_COLOR"] = "1";
        process.StartInfo.ArgumentList.Add(ApplicationPath);

        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);

        return new ProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);

    private sealed class ProcessTestWorkspace : IDisposable
    {
        public ProcessTestWorkspace()
        {
            BasePath = Path.Combine(
                Path.GetTempPath(),
                "DotnetCleanup.IntegrationTests",
                Guid.NewGuid().ToString("N"));
            RootPath = Path.Combine(BasePath, "root");
            TempPath = Path.Combine(BasePath, "temp");

            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(TempPath);
        }

        public string BasePath { get; }

        public string RootPath { get; }

        public string TempPath { get; }

        public string CreateRootDirectory(params string[] segments)
        {
            var path = Path.Combine([RootPath, .. segments]);
            Directory.CreateDirectory(path);
            return path;
        }

        public string[] GetTempRunDirectories()
        {
            return Directory.Exists(TempPath)
                ? Directory.GetDirectories(TempPath, "~dotnetcleanup-*", SearchOption.TopDirectoryOnly)
                : [];
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(BasePath))
                {
                    Directory.Delete(BasePath, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
