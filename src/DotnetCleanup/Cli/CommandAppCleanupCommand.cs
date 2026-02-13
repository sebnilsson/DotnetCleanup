using Spectre.Console.Cli;

namespace DotnetCleanup.Cli;

public static class CommandAppCleanupCommand
{
    public static readonly Action<IConfigurator> Configurator = (config) =>
    {
        config.SetApplicationName("dotnet-cleanup");

        config.AddExample([@"c:\src\project", "--include", "**/bin", "--include", "**/obj", "--include", "**/node_modules", "--exclude", "README.md"]);
        config.AddExample(["-p", "**/bin", "-p", "**/obj", "-y"]);
        config.AddExample(["-p", "**/node_modules", "--verbosity", "minimal"]);
#if DEBUG
        config.PropagateExceptions();
        config.ValidateExamples();
#endif
    };
}
