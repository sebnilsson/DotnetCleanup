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
            _log.Detailed("Cleanup started", ConsoleColors.Info);
            _log.Detailed();

            return Task.CompletedTask;
        }

        public Task Handle(
            ApplicationEnd notification, 
            CancellationToken cancellationToken)
        {
            _log.Detailed();
            _log.Detailed("Cleanup ended", ConsoleColors.Info);

            return Task.CompletedTask;
        }
    }
}