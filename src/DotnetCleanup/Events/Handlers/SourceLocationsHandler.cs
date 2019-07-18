using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

namespace DotnetCleanup.Events.Handlers
{
    public class SourceLocationsHandler
        : INotificationHandler<SourceLocationCompleted>
    {
        private readonly ILog _log;

        public SourceLocationsHandler(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task Handle(
            SourceLocationCompleted notification,
            CancellationToken cancellationToken)
        {
            _log.Detailed($"Source locations:", ConsoleColors.Info);

            foreach (var path in notification.SourceLocations)
            {
                _log.Detailed(path.Value);
            }

            _log.Detailed();

            return Task.CompletedTask;
        }
    }
}
