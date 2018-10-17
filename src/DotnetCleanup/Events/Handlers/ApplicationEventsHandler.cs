using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

namespace DotnetCleanup.Events.Handlers
{
    public class ApplicationEventsHandler
        : INotificationHandler<ApplicationStart>,
        INotificationHandler<ApplicationEnd>
    {
        private readonly ILog _log;

        public ApplicationEventsHandler(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task Handle(
            ApplicationStart notification,
            CancellationToken cancellationToken)
        {
            var version = GetAssemblyVersion();

            _log.Normal($"DotnetCleanup v{version}", ConsoleColors.Info);
            _log.Normal();

            return Task.CompletedTask;
        }

        public Task Handle(
            ApplicationEnd notification,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static string GetAssemblyVersion()
        {
            var version = typeof(Program).Assembly.GetName()?.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}