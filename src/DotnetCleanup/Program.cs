using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DotnetCleanup;

public static class Program
{
    public static int Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });

        var logger = loggerFactory.CreateLogger<CleanupService>();
        var app = CleanupApp.Build(AnsiConsole.Console, logger, new PhysicalFileSystem());

        var normalizedArgs = CleanupApp.NormalizeArgs(args);
        return app.Run(normalizedArgs);
    }
}
