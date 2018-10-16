using System.Collections.Generic;
using System.IO;

namespace DotnetCleanup.SourceLocations
{
    internal class SolutionFilePathProvider
    {
        public IEnumerable<string> TryGetSourcePaths(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                return null;

            var isSolution =
                SolutionFileUtility.IsPathSolution(sourcePath);

            return isSolution
                ? SolutionFileParser.GetSolutionProjectPaths(sourcePath)
                : null;
        }
    }
}