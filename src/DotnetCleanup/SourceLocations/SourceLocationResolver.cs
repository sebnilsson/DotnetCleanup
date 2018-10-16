using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotnetCleanup.SourceLocations
{
    internal class SourceLocationResolver
    {
        private readonly SolutionFilePathProvider _solutionFilePathProvider;
        private readonly ProjectFilePathProvider _projectFilePathProvider;
        private readonly ProjectDirectoryPathProvider _projectDirectoryPathProvider;

        public SourceLocationResolver(
            SolutionFilePathProvider solutionFilePathProvider,
            ProjectFilePathProvider projectFilePathProvider,
            ProjectDirectoryPathProvider projectDirectoryPathProvider)
        {
            _solutionFilePathProvider = solutionFilePathProvider
                ?? throw new ArgumentNullException(nameof(solutionFilePathProvider));
            _projectFilePathProvider = projectFilePathProvider
                ?? throw new ArgumentNullException(nameof(projectFilePathProvider));
            _projectDirectoryPathProvider = projectDirectoryPathProvider
                ?? throw new ArgumentNullException(nameof(projectDirectoryPathProvider));
        }

        public IReadOnlyCollection<string> GetProjectPaths(string sourcePath)
        {
            var pathCandidates = new[]
            {
                _solutionFilePathProvider.TryGetSourcePaths(sourcePath),
                _projectFilePathProvider.TryGetSourcePaths(sourcePath),
                _projectDirectoryPathProvider.TryGetSourcePaths(sourcePath)
            };

            var paths = pathCandidates
                .Where(x => x?.Any() ?? false)
                .Select(x => x.ToList())
                .FirstOrDefault();

            if (paths == null)
                throw new FileNotFoundException(
                    $"No directory or file found at path '{sourcePath}'.");

            return paths;
        }
    }
}