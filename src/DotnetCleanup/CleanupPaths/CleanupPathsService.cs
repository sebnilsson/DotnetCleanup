using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DotnetCleanup.Events;
using MediatR;

namespace DotnetCleanup.CleanupPaths
{
    internal class CleanupPathsService : ICleanupPathsService
    {
        private readonly ILog _log;
        private readonly IMediator _mediator;
        private readonly ProjectPathResolver _projectPathResolver;

        public CleanupPathsService(
            ILog log,
            IMediator mediator,
            ProjectPathResolver projectPathResolver)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _mediator = mediator
                ?? throw new ArgumentNullException(nameof(mediator));
            _projectPathResolver = projectPathResolver
                ?? throw new ArgumentNullException(nameof(projectPathResolver));
        }

        public async Task<IReadOnlyCollection<PathInfo>> GetCleanupPaths(
            IReadOnlyCollection<PathInfo> sourcePaths)
        {
            var cleanupPaths = _projectPathResolver
                .GetProjectCleanupPaths(sourcePaths)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Select(x => new PathInfo(x))
                .Where(x => Path.IsPathFullyQualified(x.Value))
                .Where(x => x.Exists)
                .OrderByDescending(x => !x.IsFile)
                .ThenBy(x => x.Value)
                .ToList();

            var paths = GetNonNestedPaths(cleanupPaths).ToList();

            await _mediator.Publish(new CleanupPathsCompleted(paths));

            return paths;
        }

        private IEnumerable<PathInfo> GetNonNestedPaths(
            IReadOnlyCollection<PathInfo> paths)
        {
            foreach (var path in paths)
            {
                var hasParent = paths.Any(
                    x => x != path && path.Value.StartsWith(x.Value));

                if (!hasParent)
                {
                    yield return path;
                }
                else
                {
                    _log.Debug($"Nested path: {path.Value}");
                }
            }
        }
    }
}