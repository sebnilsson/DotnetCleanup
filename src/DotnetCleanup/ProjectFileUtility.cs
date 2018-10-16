using System.IO;
using System.Linq;

namespace DotnetCleanup
{
    public static class ProjectFileUtility
    {
        public static readonly string[] ProjectExtensions = new[]
        {
            ".csproj", ".fsharp", ".vbproj"
        };

        public static bool IsPathProject(string path)
        {
            var fileExtension = Path.GetExtension(path);

            return IsExtensionProject(fileExtension);
        }

        public static bool IsExtensionProject(string fileExtension)
        {
            return ProjectExtensions.Any(
                p => p.EqualsIgnoreCase(fileExtension));
        }
    }
}