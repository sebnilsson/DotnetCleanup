namespace DotnetCleanup.SourceLocations
{
    internal class SolutionFilePathProvider
    {
        public IEnumerable<string> TryGetSourcePaths(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                return [];

            var isSolution =
                SolutionFileUtility.IsPathSolution(sourcePath);

            return isSolution
                ? SolutionFileParser.GetSolutionProjectPaths(sourcePath)
                : [];
        }
    }
}
