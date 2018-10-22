using System.Collections.Generic;
using System.Linq;

namespace DotnetCleanup
{
    internal static class CommandContextPathUtility
    {
        private static readonly IReadOnlyCollection<string>
            DefaultCleanupPaths = new[]
        {
            "bin", "obj", "node_modules"
        };

        public static IReadOnlyCollection<string> GetPaths(
            IEnumerable<string> paths)
        {
            return (paths?.Any() ?? false)
                ? paths
                    .Select(PathUtility.GetCleanPath)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()
                : DefaultCleanupPaths;
        }


    }
}