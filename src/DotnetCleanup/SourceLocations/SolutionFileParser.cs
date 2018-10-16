using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotnetCleanup.SourceLocations
{
    internal static class SolutionFileParser
    {
        private static readonly Regex SolutionProjectsRegex =
            new Regex(@"^Project\(""[^""]*""\)[^""]*""[^""]*"",[^""]*""([^""]*)"".*$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static IEnumerable<string> GetSolutionProjectPaths(
            string solutionFilePath)
        {
            if (!File.Exists(solutionFilePath))
                yield break;

            var fileContent = File.ReadAllText(solutionFilePath);

            var matches = SolutionProjectsRegex
                .Matches(fileContent)
                .OfType<Match>()
                .ToList();

            var solutionDirectory = Path.GetDirectoryName(solutionFilePath);

            foreach (var match in matches)
            {
                var value = match
                    .Groups
                    .Select(x => x.Value)
                    .ElementAtOrDefault(1);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    var cleanValue = PathUtility.GetCleanPath(value);

                    var projectPath = Path.Combine(
                        solutionDirectory,
                        cleanValue);

                    if (Path.HasExtension(projectPath))
                        yield return projectPath;
                }
            }
        }
    }
}