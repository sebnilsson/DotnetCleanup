using DotnetCleanup.Cleanup;

using MediatR;

namespace DotnetCleanup.Events
{
    public class CleanupCompleted : INotification
    {
        public CleanupCompleted(CleanupResult result)
        {
            Result = result;
        }

        public CleanupResult Result { get; }
    }
}