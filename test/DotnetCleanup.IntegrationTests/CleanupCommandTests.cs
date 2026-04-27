using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotnetCleanup;
using Xunit;

namespace DotnetCleanup.IntegrationTests;

public sealed class CleanupCommandTests
{
    [Fact]
    public async Task Run_WithYesAndNoop_LeavesMatchedDirectoryUntouched()
    {
        // Arrange
        await using var workspace = await ProcessTestWorkspace.CreateAsync(TestContext.Current.CancellationToken);
        var binPath = await workspace.CreateRootDirectoryAsync(["projectA", "bin"], TestContext.Current.CancellationToken);

        // Act
        var result = await RunAppAsync(
            workspace,
            [
                workspace.RootPath,
                "--temp-path", workspace.TempPath,
                "-p", "**/bin",
                "-y",
                "--noop"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(await workspace.DirectoryExistsAsync(binPath, TestContext.Current.CancellationToken));
        Assert.Empty(await workspace.GetTempRunDirectoriesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task Run_WithoutPathArgument_UsesCurrentDirectoryAndLeavesStagedPathsWhenNoDelete()
    {
        // Arrange
        await using var workspace = await ProcessTestWorkspace.CreateAsync(TestContext.Current.CancellationToken);
        var binPath = await workspace.CreateRootDirectoryAsync(["projectA", "bin"], TestContext.Current.CancellationToken);

        // Act
        var result = await RunAppAsync(
            workspace,
            [
                "--temp-path", workspace.TempPath,
                "-p", "**/bin",
                "-y",
                "--no-delete"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var tempRunPath = Assert.Single(await workspace.GetTempRunDirectoriesAsync(TestContext.Current.CancellationToken));

        Assert.False(await workspace.DirectoryExistsAsync(binPath, TestContext.Current.CancellationToken));
        Assert.True(await workspace.DirectoryExistsAsync(workspace.Combine(tempRunPath, "projectA", "bin"), TestContext.Current.CancellationToken));
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task Run_WithBracketedPaths_DeletesMatchedDirectory()
    {
        // Arrange
        await using var workspace = await ProcessTestWorkspace.CreateAsync(TestContext.Current.CancellationToken);
        var binPath = await workspace.CreateRootDirectoryAsync(["project[1]", "bin"], TestContext.Current.CancellationToken);

        // Act
        var result = await RunAppAsync(
            workspace,
            [
                workspace.RootPath,
                "--temp-path", workspace.TempPath,
                "-p", "**/bin",
                "-y",
                "--verbosity", "detailed"
            ]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.False(await workspace.DirectoryExistsAsync(binPath, TestContext.Current.CancellationToken));
        Assert.Empty(await workspace.GetTempRunDirectoriesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task Run_NonExistingRootPath_ReturnsErrorResult()
    {
        // Arrange
        await using var workspace = await ProcessTestWorkspace.CreateAsync(TestContext.Current.CancellationToken);
        var missingPath = workspace.Combine(workspace.RootPath, "missing-root");

        // Act
        var result = await RunAppAsync(
            workspace,
            [
                missingPath,
                "--temp-path", workspace.TempPath,
                "-y",
                "--noop"
            ]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Error);
    }

    private static string ApplicationDirectory => Path.GetDirectoryName(typeof(CleanupService).Assembly.Location)
        ?? throw new InvalidOperationException("Unable to locate application output directory.");

    private static async Task<ProcessResult> RunAppAsync(ProcessTestWorkspace workspace, string[] args)
    {
        return await workspace.RunAppAsync(args, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private sealed record ProcessResult(long ExitCode, string Output, string Error);

    private sealed class ProcessTestWorkspace : IAsyncDisposable
    {
        public const string ApplicationPath = "/app/DotnetCleanup.dll";

        private const string AppDirectory = "/app";

        private const string RootDirectory = "/workspace/root";

        private const string TempDirectory = "/workspace/temp";

        private ProcessTestWorkspace(IContainer container)
        {
            Container = container;
            RootPath = RootDirectory;
            TempPath = TempDirectory;
        }

        public IContainer Container { get; }

        public string RootPath { get; }

        public string TempPath { get; }

        public static async Task<ProcessTestWorkspace> CreateAsync(CancellationToken cancellationToken)
        {
            var container = new ContainerBuilder(DotnetSdkImage)
                .WithBindMount(ApplicationDirectory, AppDirectory, AccessMode.ReadOnly)
                .WithEntrypoint("tail")
                .WithCommand("-f", "/dev/null")
                .WithEnvironment("NO_COLOR", "1")
                .WithCleanUp(true)
                .Build();

            await container.StartAsync(cancellationToken).ConfigureAwait(false);

            var workspace = new ProcessTestWorkspace(container);
            await workspace.ExecShellAsync(
                $"mkdir -p -- {Quote(workspace.RootPath)} {Quote(workspace.TempPath)}",
                cancellationToken)
                .ConfigureAwait(false);

            return workspace;
        }

        public async Task<string> CreateRootDirectoryAsync(string[] segments, CancellationToken cancellationToken)
        {
            var path = Combine([RootPath, .. segments]);
            await ExecShellAsync($"mkdir -p -- {Quote(path)}", cancellationToken).ConfigureAwait(false);
            return path;
        }

        public async Task<ProcessResult> RunAppAsync(string[] args, CancellationToken cancellationToken)
        {
            var appCommand = string.Join(
                " ",
                new[] { "dotnet", ApplicationPath }
                    .Concat(args)
                    .Select(Quote));
            var result = await ExecShellAsync($"cd {Quote(RootPath)} && exec {appCommand}", cancellationToken).ConfigureAwait(false);

            return new ProcessResult(result.ExitCode.GetValueOrDefault(-1), result.Stdout, result.Stderr);
        }

        public async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken)
        {
            var result = await ExecShellAsync($"test -d {Quote(path)}", cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0;
        }

        public async Task<string[]> GetTempRunDirectoriesAsync(CancellationToken cancellationToken)
        {
            var result = await ExecShellAsync(
                $"find {Quote(TempPath)} -mindepth 1 -maxdepth 1 -type d -name '~dotnetcleanup-*' -print",
                cancellationToken)
                .ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return [];
            }

            return result.Stdout
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public string Combine(params string[] segments)
        {
            var combinedPath = string.Join("/", segments.Select(segment => segment.Trim('/')));
            return segments[0].StartsWith("/", StringComparison.Ordinal)
                ? "/" + combinedPath
                : combinedPath;
        }

        public async ValueTask DisposeAsync()
        {
            await Container.DisposeAsync().ConfigureAwait(false);
        }

        private static string DotnetSdkImage =>
#if NET10_0_OR_GREATER
            "mcr.microsoft.com/dotnet/sdk:10.0";
#else
            "mcr.microsoft.com/dotnet/sdk:9.0";
#endif

        private static string Quote(string value) => "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

        private Task<ExecResult> ExecShellAsync(string command, CancellationToken cancellationToken)
        {
            return Container.ExecAsync(["/bin/sh", "-c", command], cancellationToken);
        }
    }
}
