using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCleanup.SourceLocations
{
    public interface ISourceLocationService
    {
        Task<IReadOnlyCollection<PathInfo>> GetSourceLocations(
            string sourcePath);
    }
}