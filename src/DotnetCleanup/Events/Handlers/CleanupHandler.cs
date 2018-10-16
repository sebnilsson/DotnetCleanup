using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KeyLocks;
using MediatR;

namespace DotnetCleanup.Events.Handlers
{
    public class CleanupHandler :
        INotificationHandler<CleanupCompleted>,
        INotificationHandler<CleanupSuccess>,
        INotificationHandler<CleanupError>
    {
        private readonly ILog _log;
        private readonly KeyLock<ILog> _logLock;

        public CleanupHandler(ILog log, KeyLock<ILog> logLock)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _logLock = logLock
                ?? throw new ArgumentNullException(nameof(logLock));
        }

        public Task Handle(
            CleanupCompleted notification,
            CancellationToken cancellationToken)
        {
            var successes = notification.Result.Successes;
            var errors = notification.Result.Errors;

            _log.Normal();

            if (successes.Any())
            {
                _log.Minimal(
                    $"Paths cleaned: {successes.Count}",
                    ConsoleColors.Success);
            }

            if (errors.Any())
            {
                _log.Minimal(
                    $"Paths failed: {errors.Count}",
                    ConsoleColors.Error);
            }

            return Task.CompletedTask;
        }

        public Task Handle(
            CleanupSuccess success,
            CancellationToken cancellationToken)
        {
            var cleanupPath = success.CleanupPath.Value;
            var movePath = success.MovePath.Value;

            _logLock.RunWithLock(_log, () =>
            {
                _log.Normal(cleanupPath, ConsoleColors.Success);

                if (cleanupPath != movePath)
                {
                    _log.Detailed($"> From: {movePath}", ConsoleColors.Info);
                }
            });

            return Task.CompletedTask;
        }

        public Task Handle(CleanupError error, CancellationToken cancellationToken)
        {
            _logLock.RunWithLock(_log, () =>
            {
                _log.Normal(error.CleanupPath.Value, ConsoleColors.Error);
                _log.Normal(
                    $"> {error.Exception.Message}",
                    ConsoleColors.Info);

                _log.Detailed(error.Exception.StackTrace);
            });

            return Task.CompletedTask;
        }
    }
}