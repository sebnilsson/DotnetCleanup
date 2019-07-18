using System;
using System.Linq;
using System.Threading.Tasks;

using DotnetCleanup.Cleanup;
using DotnetCleanup.CleanupPaths;
using DotnetCleanup.Events;
using DotnetCleanup.PostCleanup;
using DotnetCleanup.SourceLocations;
using McMaster.Extensions.CommandLineUtils;
using MediatR;

namespace DotnetCleanup
{
    public class ApplicationService
    {
        private readonly CommandContext _context;
        private readonly ILog _log;
        private readonly IMediator _mediator;
        private readonly ICleanupService _cleanupService;
        private readonly ICleanupPathsService _cleanupPathsService;
        private readonly ISourceLocationService _sourceLocationService;
        private readonly IPostCleanupService _postCleanupService;

        public ApplicationService(
            CommandContext context,
            ILog log,
            IMediator mediator,
            ISourceLocationService sourceLocationService,
            ICleanupPathsService cleanupPathsService,
            ICleanupService cleanupService,
            IPostCleanupService postCleanupService)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _mediator = mediator
                ?? throw new ArgumentNullException(nameof(mediator));
            _sourceLocationService = sourceLocationService
                ?? throw new ArgumentNullException(nameof(sourceLocationService));
            _cleanupPathsService = cleanupPathsService
                ?? throw new ArgumentNullException(nameof(cleanupPathsService));
            _cleanupService = cleanupService
                ?? throw new ArgumentNullException(nameof(cleanupService));
            _postCleanupService = postCleanupService
                ?? throw new ArgumentNullException(nameof(postCleanupService));
        }

        public async Task OnExecuteAsync()
        {
            await _mediator.Publish(new ApplicationStart());

            var sourceLocations =
                await _sourceLocationService.GetSourceLocations(_context.Path);

            var cleanupPaths =
                await _cleanupPathsService.GetCleanupPaths(sourceLocations);

            if (cleanupPaths.Any())
            {
                var isCleanupConfirmed = ConfirmCleanup();
                if (isCleanupConfirmed)
                {
                    var result = await _cleanupService.Cleanup(cleanupPaths);

                    await _postCleanupService.PostCleanup(result);
                }
            }

            await _mediator.Publish(new ApplicationEnd());
        }

        private bool ConfirmCleanup()
        {
            if (_context.ConfirmCleanup)
            {
                _log.Debug("Cleanup automatically confirmed by command-argument");
                return true;
            }

            _log.Normal();

            var isCleanupConfirmed = Prompt.GetYesNo(
                "Do you want to clean up these paths?",
                defaultAnswer: false,
                promptColor: ConsoleColors.Warning.Foreground,
                promptBgColor: ConsoleColors.Warning.Background);

            _log.Normal();

            if (isCleanupConfirmed)
                _log.Debug("Cleanup confirmed");
            else
                _log.Debug("Cleanup cancelled");

            return isCleanupConfirmed;
        }
    }
}
