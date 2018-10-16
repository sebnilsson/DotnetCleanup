using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

namespace DotnetCleanup.Events.Handlers
{
    public class CleanupPathsHandler :
        INotificationHandler<CleanupPathsCompleted>
    {
        private readonly ILog _log;

        public CleanupPathsHandler(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task Handle(
            CleanupPathsCompleted notification,
            CancellationToken cancellationToken)
        {
            if (!notification.Paths.Any())
            {
                _log.Minimal(
                    "No paths found for cleanup",
                    ConsoleColors.Warning);
            }
            else
            {
                _log.Normal($"Cleanup paths:", ConsoleColors.Info);

                foreach (var path in notification.Paths)
                {
                    _log.Minimal(path.Value);
                }
            }

            return Task.CompletedTask;
        }
    }
}