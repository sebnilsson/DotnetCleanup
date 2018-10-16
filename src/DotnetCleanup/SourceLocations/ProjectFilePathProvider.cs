using System.Collections.Generic;
using System.IO;

namespace DotnetCleanup.SourceLocations
{
    internal class ProjectFilePathProvider
    {
        public IEnumerable<string> TryGetSourcePaths(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                return null;

            var isProject = ProjectFileUtility.IsPathProject(sourcePath);

            return isProject ? new[] { sourcePath } : null;
        }
    }
}