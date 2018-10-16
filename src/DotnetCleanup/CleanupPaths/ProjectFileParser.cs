using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DotnetCleanup.CleanupPaths
{
    internal static class ProjectFileParser
    {
        public static IEnumerable<string> GetProjectFilePaths(
            string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
                yield break;

            var xmlDocument = XDocument.Load(projectFilePath);

            var xmlPaths = xmlDocument
                .Element("Project")?
                .Elements("PropertyGroup")?
                .Descendants(@"OutputPath")?
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                    ?? Enumerable.Empty<string>();

            var projectDirectory = Path.GetDirectoryName(projectFilePath);

            foreach (var path in xmlPaths)
                yield return path;
        }
    }
}
