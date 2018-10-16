namespace DotnetCleanup
{
    internal static class DescriptionTexts
    {
        public const string CleanPaths = "Defines the paths to clean. Defaults to 'bin', 'obj' and 'node_modules'.";

        public const string ConfirmCleanup = "Confirm prompt for file cleanup automatically.";

        public const string NoDelete = "Defines if files should be deleted, after confirmation.";

        public const string NoMove = "Defines if files should be moved before deletion, after confirmation.";

        public const string Path = "Path to solution/project/folder to clean. Defaults to current working directory.";

        public const string TempPath = "Directory used to move files to before delete. Defaults to system Temp-folder.";

        public const string Verbosity = "Sets the verbosity of the command. Allowed: Minimal, Normal, Detailed and Debug.";
    }
}