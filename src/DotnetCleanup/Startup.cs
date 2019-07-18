using System;

using DotnetCleanup.Cleanup;
using DotnetCleanup.CleanupPaths;
using DotnetCleanup.Events;
using DotnetCleanup.Events.Handlers;
using DotnetCleanup.PostCleanup;
using DotnetCleanup.SourceLocations;
using KeyLocks;
using McMaster.Extensions.CommandLineUtils;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetCleanup
{
    internal static class Startup
    {
        public static IServiceProvider ConfigureServices(
            Func<IServiceProvider, CommandContext> contextFactoy)
        {
            var services = new ServiceCollection()
                // Infrastructure
                .AddSingleton(PhysicalConsole.Singleton)
                .AddSingleton(contextFactoy)
                .AddSingleton<ILog, Log>()
                .AddSingleton<KeyLock<ILog>>()
                .AddSingleton<IMediator, Mediator>()
                .AddTransient<ServiceFactory>(x =>
                {
                    var c = x.GetRequiredService<IServiceProvider>();
                    return t => c.GetRequiredService(t);
                })
                // Event-handlers
                .AddScoped<INotificationHandler<ApplicationEnd>, ApplicationEventsHandler>()
                .AddScoped<INotificationHandler<ApplicationStart>, ApplicationEventsHandler>()
                .AddScoped<INotificationHandler<CleanupCompleted>, CleanupHandler>()
                .AddScoped<INotificationHandler<CleanupSuccess>, CleanupHandler>()
                .AddScoped<INotificationHandler<CleanupError>, CleanupHandler>()
                .AddScoped<INotificationHandler<CleanupPathsCompleted>, CleanupPathsHandler>()
                .AddScoped<INotificationHandler<SourceLocationCompleted>, SourceLocationsHandler>()
                // SourceResolution
                .AddTransient<ISourceLocationService, SourceLocationService>()
                .AddTransient<ProjectDirectoryPathProvider>()
                .AddTransient<ProjectFilePathProvider>()
                .AddTransient<SolutionFilePathProvider>()
                .AddTransient<SourceLocationResolver>()
                // PathsResolution
                .AddTransient<ICleanupPathsService, CleanupPathsService>()
                .AddTransient<ProjectPathResolver>()
                // Cleanup
                .AddTransient<ICleanupService, CleanupService>()
                .AddTransient<IPostCleanupService, PostCleanupService>()
                .AddTransient<DeletionHelper>()
                .AddTransient<MovingHelper>()
                // Application
                .AddTransient<ApplicationService>();

            return services.BuildServiceProvider();
        }
    }
}
