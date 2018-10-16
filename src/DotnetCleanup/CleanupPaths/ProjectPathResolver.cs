using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotnetCleanup.CleanupPaths
{
    internal class ProjectPathResolver
    {
        private readonly CommandContext _context;

        public ProjectPathResolver(CommandContext context)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        public IReadOnlyCollection<string> GetProjectCleanupPaths(
            IEnumerable<PathInfo> sourcePaths)
        {
            return sourcePaths
                .SelectMany(x => GetProjectCleanupPaths(x))
                .ToList();
        }

        public IEnumerable<string> GetProjectCleanupPaths(PathInfo sourcePath)
        {
            var directoryPath = sourcePath.IsFile
                ? Path.GetDirectoryName(sourcePath.Value)
                : sourcePath.Value;

            var projectPaths =
                ProjectFileUtility.IsPathProject(sourcePath.Value)
                    ? ProjectFileParser.GetProjectFilePaths(sourcePath.Value)
                    : Enumerable.Empty<string>();

            foreach (var projectPath in projectPaths)
            {
                yield return Path.Combine(directoryPath, projectPath);
            }

            foreach (var cleanPath in _context.CleanPaths)
            {
                yield return Path.Combine(directoryPath, cleanPath);
            }
        }
    }
}