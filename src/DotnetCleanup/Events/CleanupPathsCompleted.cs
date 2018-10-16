using System.Collections.Generic;

using MediatR;

namespace DotnetCleanup.Events
{
    public class CleanupPathsCompleted : INotification
    {
        public CleanupPathsCompleted(IReadOnlyCollection<PathInfo> paths)
        {
            Paths = paths;
        }

        public IReadOnlyCollection<PathInfo> Paths { get; }
    }
}