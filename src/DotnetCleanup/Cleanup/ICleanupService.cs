namespace DotnetCleanup.Cleanup
{
    public interface ICleanupService
    {
        Task<CleanupResult> Cleanup(IEnumerable<PathInfo> cleanupPaths);
    }
}
