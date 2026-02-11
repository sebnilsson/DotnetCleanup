namespace DotnetCleanup;

public sealed record CleanupStep
{
    private readonly Lock _pathsLock = new();

    public ISet<PathInfo> Successes { get; } = new HashSet<PathInfo>(new PathInfoComparer());

    public ISet<PathInfo> Failed { get; } = new HashSet<PathInfo>(new PathInfoComparer());

    public bool AddSuccess(PathInfo path)
    {
        lock (_pathsLock)
        {
            return Successes.Add(path);
        }
    }

    public bool AddFailed(PathInfo path)
    {
        lock (_pathsLock)
        {
            return Failed.Add(path);
        }
    }

    private sealed class PathInfoComparer : IEqualityComparer<PathInfo>
    {
        private static readonly StringComparer s_pathComparer =
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        public bool Equals(PathInfo? x, PathInfo? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (x is null || y is null)
            {
                return false;
            }

            return s_pathComparer.Equals(x.InitialValue, y.InitialValue);
        }

        public int GetHashCode(PathInfo obj)
        {
            return s_pathComparer.GetHashCode(obj.InitialValue ?? string.Empty);
        }
    }
}
