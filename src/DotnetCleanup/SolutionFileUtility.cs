using System.IO;

namespace DotnetCleanup
{
    internal static class SolutionFileUtility
    {
        private const string SolutionExtension = ".sln";

        public static bool IsPathSolution(string path)
        {
            var fileExtension = Path.GetExtension(path);

            return IsExtensionSolution(fileExtension);
        }

        public static bool IsExtensionSolution(string fileExtension)
        {
            return fileExtension.EqualsIgnoreCase(SolutionExtension);
        }
    }
}