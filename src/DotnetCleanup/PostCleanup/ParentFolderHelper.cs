using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetCleanup.PostCleanup
{
    internal static class ParentFolderHelper
    {
        public static IReadOnlyList<PathInfo> GetParentFolders(
            IEnumerable<PathInfo> paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            return
                paths
                    .Select(x => PathUtility.GetParentPath(x.Value))
                    .Distinct()
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new PathInfo(x))
                    .ToList();
        }
    }
}
