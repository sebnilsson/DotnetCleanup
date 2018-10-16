using System;

using MediatR;

namespace DotnetCleanup.Events
{
    public class CleanupError : INotification
    {
        public CleanupError(PathInfo cleanupPath, Exception exception)
        {
            CleanupPath = cleanupPath;
            Exception = exception;
        }

        public PathInfo CleanupPath { get; }

        public Exception Exception { get; }
    }
}