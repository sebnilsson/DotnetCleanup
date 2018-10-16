using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DotnetCleanup.Events;
using MediatR;

namespace DotnetCleanup.SourceLocations
{
    internal class SourceLocationService : ISourceLocationService
    {
        private readonly CommandContext _context;
        private readonly IMediator _mediator;
        private readonly SourceLocationResolver _sourcePathResolver;

        public SourceLocationService(
            CommandContext context,
            SourceLocationResolver sourcePathResolver,
            IMediator mediator)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
            _sourcePathResolver = sourcePathResolver
                ?? throw new ArgumentNullException(nameof(sourcePathResolver));
            _mediator = mediator
                ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<IReadOnlyCollection<PathInfo>> GetSourceLocations(
            string sourcePath)
        {
            var sourceLocations = _sourcePathResolver.GetProjectPaths(sourcePath)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Select(x => new PathInfo(x))
                .OrderByDescending(x => !x.IsFile)
                .ThenBy(x => x.Value)
                .ToList();

            await _mediator.Publish(new SourceLocationCompleted(sourceLocations));

            return sourceLocations;
        }
    }
}