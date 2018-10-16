using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DotnetCleanup.Events;
using MediatR;

namespace DotnetCleanup.Cleanup
{
    internal class CleanupService : ICleanupService
    {
        private readonly CommandContext _context;
        private readonly DeletionHelper _deletionHelper;
        private readonly IMediator _mediator;
        private readonly MovingHelper _movingHelper;

        public CleanupService(
            CommandContext context,
            DeletionHelper deletionHelper,
            MovingHelper movingHelper,
            IMediator mediator)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
            _deletionHelper = deletionHelper
                ?? throw new ArgumentNullException(nameof(deletionHelper));
            _movingHelper = movingHelper
                ?? throw new ArgumentNullException(nameof(movingHelper));
            _mediator = mediator
                ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<CleanupResult> Cleanup(
            IEnumerable<PathInfo> cleanupPaths)
        {
            var result = new CleanupResult();

            var cleanupTasks = cleanupPaths
                .ToList()
                .Select(x => Task.Run(
                    () => Cleanup(x, result)));

            await Task.WhenAll(cleanupTasks);

            await _mediator.Publish(new CleanupCompleted(result));

            return result;
        }

        private void Cleanup(PathInfo cleanupPath, CleanupResult result)
        {
            try
            {
                var movePath = _movingHelper.Move(cleanupPath);

                _deletionHelper.Delete(movePath);

                result.AddSuccess(cleanupPath, movePath);

                _mediator.Publish(new CleanupSuccess(cleanupPath, movePath));
            }
            catch (Exception ex)
            {
                result.AddError(cleanupPath, ex);

                _mediator.Publish(new CleanupError(cleanupPath, ex));
            }
        }
    }
}