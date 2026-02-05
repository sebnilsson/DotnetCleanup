namespace DotnetCleanup;

public sealed record CleanupStep
{
    public ISet<PathInfo> Successes { get; } = new HashSet<PathInfo>(new PathInfoComparer());

    public ISet<PathInfo> Errors { get; } = new HashSet<PathInfo>(new PathInfoComparer());

    public bool AddSuccess(PathInfo path) => Successes.Add(path);

    public bool AddError(PathInfo path) => Errors.Add(path);

    private class PathInfoComparer : IEqualityComparer<PathInfo>
    {
        private static StringComparer GetPathComparer() =>
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        public bool Equals(PathInfo x, PathInfo y)
        {
            var pathComparer = GetPathComparer();
            return pathComparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(PathInfo obj)
        {
            var pathComparer = GetPathComparer();
            return pathComparer.GetHashCode(obj.Value ?? string.Empty);
        }
    }
}
