using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCleanup.CleanupPaths
{
    public interface ICleanupPathsService
    {
        Task<IReadOnlyCollection<PathInfo>> GetCleanupPaths(
            IReadOnlyCollection<PathInfo> sourcePaths);
    }
}