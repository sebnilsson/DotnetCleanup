namespace DotnetCleanup.SourceLocations
{
    internal class ProjectDirectoryPathProvider(
        SolutionFilePathProvider solutionFilePathProvider,
        ProjectFilePathProvider projectFilePathProvider)
    {
        private readonly SolutionFilePathProvider _solutionFilePathProvider = solutionFilePathProvider
                ?? throw new ArgumentNullException(nameof(solutionFilePathProvider));
        private readonly ProjectFilePathProvider _projectFilePathProvider = projectFilePathProvider
                ?? throw new ArgumentNullException(nameof(projectFilePathProvider));

        public IEnumerable<string> TryGetSourcePaths(string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
                return [];

            var files = Directory.GetFiles(sourcePath);

            var solutionFilePath = TryGetSolutionFilePath(sourcePath, files);
            if (solutionFilePath != null)
            {
                return _solutionFilePathProvider
                    .TryGetSourcePaths(solutionFilePath);
            }

            var projectFilePath = TryGetProjectFilePath(sourcePath, files);
            if (projectFilePath != null)
            {
                return _projectFilePathProvider
                    .TryGetSourcePaths(projectFilePath);
            }

            return [sourcePath];
        }

        private static string? TryGetSolutionFilePath(
            string sourcePath,
            IReadOnlyCollection<string> files)
        {
            try
            {
                return TryGetSingleFilePathOfType(
                    SolutionFileUtility.IsPathSolution,
                    sourcePath,
                    files);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new ArgumentOutOfRangeException(
                       $"Multiple solution-files found in '{sourcePath}'.",
                       ex);
            }
        }

        private static string? TryGetProjectFilePath(
            string sourcePath,
            IReadOnlyCollection<string> files)
        {
            try
            {
                return TryGetSingleFilePathOfType(
                    ProjectFileUtility.IsPathProject,
                    sourcePath,
                    files);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new ArgumentOutOfRangeException(
                       $"Multiple project-files found in '{sourcePath}'.",
                       ex);
            }
        }

        private static string? TryGetSingleFilePathOfType(
            Func<string, bool> typePredicate,
            string sourcePath,
            IReadOnlyCollection<string> files)
        {
            var paths = files.Where(typePredicate)
                .Take(2)
                .ToList();

            if (paths.Count > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourcePath));
            }

            return paths.FirstOrDefault();
        }
    }
}
