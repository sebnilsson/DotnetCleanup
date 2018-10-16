using MediatR;

namespace DotnetCleanup.Events
{
    public class CleanupSuccess : INotification
    {
        public CleanupSuccess(PathInfo cleanupPath, PathInfo movePath)
        {
            CleanupPath = cleanupPath;
            MovePath = movePath;
        }

        public PathInfo CleanupPath { get; }

        public PathInfo MovePath { get; }
    }
}