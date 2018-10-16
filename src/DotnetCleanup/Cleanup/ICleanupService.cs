using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCleanup.Cleanup
{
    public interface ICleanupService
    {
        Task<CleanupResult> Cleanup(IEnumerable<PathInfo> cleanupPaths);
    }
}