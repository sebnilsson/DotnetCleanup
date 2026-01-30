using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotnetCleanup;

internal sealed class CleanupCommand : AsyncCommand<CleanupSettings>
{
    private readonly IAnsiConsole _console;
    private readonly ILogger<CleanupService> _logger;
    private readonly IFileSystem _fileSystem;

    public CleanupCommand(
        IAnsiConsole console,
        ILogger<CleanupService> logger,
        IFileSystem fileSystem)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CleanupSettings settings)
    {
        var service = new CleanupService(_console, _fileSystem, _logger);
        return await service.RunAsync(settings, CancellationToken.None);
    }
}
