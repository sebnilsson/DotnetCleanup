using System;
using System.Diagnostics;

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetCleanup
{
    [Command(Name = "cleanup", Description = "Cleans the output of solution and/or projects.")]
    [HelpOption]
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

            var app = new CommandLineApplication<CommandContext>();

            var services = Startup.ConfigureServices(_ =>
            {
                app.Model.EnsureValues(app);

                return app.Model;
            });

            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);

            app.OnExecute(async () =>
            {
                var appService =
                    services.GetRequiredService<ApplicationService>();
                await appService.OnExecuteAsync();

                OnEnd();
            });

            return ExecuteApp(app, args, services);
        }

        private static int ExecuteApp(
            CommandLineApplication<CommandContext> app,
            string[] args,
            IServiceProvider services)
        {
            var console = services.GetService<IConsole>()
                ?? PhysicalConsole.Singleton;

            try
            {
                return app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                using (console.ColorContext(ConsoleColors.Error))
                {
                    console.WriteLine($"Input error: {ex.Message}");
                    console.WriteLine();
                }

                app.ShowHelp();

                return 1;
            }
            catch (Exception ex)
            {
                using (console.ColorContext(ConsoleColors.Error))
                {
                    console.WriteLine($"Unknown error: {ex.Message}");
                }

                throw;
            }
        }

        private static void OnCancelKeyPress(
            object sender,
            ConsoleCancelEventArgs e)
        {
            Console.ResetColor();
        }

        private static void OnEnd()
        {
            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to close application...");
                Console.ReadKey(intercept: true);
            }

            Console.ResetColor();
        }
    }
}