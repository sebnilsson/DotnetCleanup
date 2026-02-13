using DotnetCleanup.Cli;
using DotnetCleanup.IO;
using DotnetCleanup.Spectre;
using Spectre.Console;
using Spectre.Console.Cli;

Console.CancelKeyPress += OnCancelKeyPress;

var typeRegistrar = GetTypeRegistrar();

var app = new CommandApp<CleanupCommand>(typeRegistrar);

app.Configure(CommandAppCleanupCommand.Configurator);

try
{
    return app.Run(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    return -99;
}

static ITypeRegistrar GetTypeRegistrar()
{
    var typeRegistrar = new SimpleTypeRegistrar();

    typeRegistrar.RegisterInstance(typeof(IFileSystem), new FileSystem());
    typeRegistrar.RegisterInstance(typeof(IAnsiConsole), AnsiConsole.Console);

    return typeRegistrar;
}

static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    Console.ResetColor();
}
