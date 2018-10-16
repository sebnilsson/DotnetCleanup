using System;
using System.IO;

namespace DotnetCleanup.Cleanup
{
    internal class DeletionHelper
    {
        private readonly CommandContext _context;

        public DeletionHelper(CommandContext context)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        public void Delete(PathInfo cleanupPath)
        {
            if (_context.NoDelete)
                return;

            if (cleanupPath.IsFile)
            {
                File.Delete(cleanupPath.Value);
            }
            else
            {
                Directory.Delete(cleanupPath.Value, recursive: true);
            }
        }
    }
}