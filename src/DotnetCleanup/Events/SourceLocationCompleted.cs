using System.Collections.Generic;

using MediatR;

namespace DotnetCleanup.Events
{
    public class SourceLocationCompleted : INotification
    {
        public SourceLocationCompleted(
            IReadOnlyCollection<PathInfo> sourceLocations)
        {
            SourceLocations = sourceLocations;
        }

        public IReadOnlyCollection<PathInfo> SourceLocations { get; }
    }
}