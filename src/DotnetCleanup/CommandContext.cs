using System;
using System.Collections.Generic;
using System.IO;

using McMaster.Extensions.CommandLineUtils;

namespace DotnetCleanup
{
    public class CommandContext
    {
        [Option("-p|--paths", DescriptionTexts.CleanPaths, CommandOptionType.MultipleValue)]
        public IReadOnlyCollection<string> CleanPaths { get; private set; }

        [Option("-y|--confirm-cleanup", DescriptionTexts.ConfirmCleanup, CommandOptionType.NoValue)]
        public bool ConfirmCleanup { get; }

        [Option("-nd|--no-delete", DescriptionTexts.NoDelete, CommandOptionType.NoValue)]
        public bool NoDelete { get; }

        [Option("-nm|--no-move", DescriptionTexts.NoMove, CommandOptionType.NoValue)]
        public bool NoMove { get; }

        [Argument(0, "PATH", DescriptionTexts.Path)]
        public string Path { get; private set; }

        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

        [Option("-t|--temp-path", DescriptionTexts.TempPath, CommandOptionType.SingleValue)]
        public string TempPath { get; private set; }

        [Option("-v|--verbosity", DescriptionTexts.Verbosity, CommandOptionType.SingleOrNoValue)]
        public LogLevel Verbosity { get; }

        internal void EnsureValues(CommandLineApplication<CommandContext> app)
        {
            CleanPaths = CommandContextPathUtility.GetPaths(CleanPaths);

            Path = !string.IsNullOrWhiteSpace(Path)
                   ? Path
                   : Directory.GetCurrentDirectory();

            Path = PathUtility.GetCleanPath(Path);

            TempPath = GetTempPath(TempPath);
        }

        private string GetTempPath(string tempPath)
        {
            var path = !string.IsNullOrWhiteSpace(tempPath)
                ? tempPath
                : System.IO.Path.GetTempPath();

            if (!Directory.Exists(path))
                throw new FileNotFoundException(
                    $"No temp-directory found at path '{path}'.");

            return path;
        }
    }
}