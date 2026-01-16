namespace DotnetCleanup.SourceLocations
{
    internal class ProjectFilePathProvider
    {
        public IEnumerable<string> TryGetSourcePaths(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                return [];

            var isProject = ProjectFileUtility.IsPathProject(sourcePath);

            return isProject ? [sourcePath] : [];
        }
    }
}
