using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetCleanup.Cleanup;

namespace DotnetCleanup.PostCleanup
{
    internal class PostCleanupService : IPostCleanupService
    {
        private readonly DeletionHelper _deletionHelper;

        public PostCleanupService(DeletionHelper deletionHelper)
        {
            _deletionHelper = deletionHelper
                ?? throw new ArgumentNullException(nameof(deletionHelper));
        }

        public async Task PostCleanup(CleanupResult cleanupResult)
        {
            var movePaths =
                cleanupResult.Successes.Select(x => x.MovePath).ToList();

            var parentFolders = ParentFolderHelper.GetParentFolders(movePaths);

            var deleteTasks =
                parentFolders
                    .Select(x => Task.Run(() => _deletionHelper.Delete(x)));

            await Task.WhenAll(deleteTasks);
        }
    }
}
