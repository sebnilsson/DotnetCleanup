using System.Threading.Tasks;
using DotnetCleanup.Cleanup;

namespace DotnetCleanup.PostCleanup
{
    public interface IPostCleanupService
    {
        Task PostCleanup(CleanupResult cleanupResult);
    }
}
