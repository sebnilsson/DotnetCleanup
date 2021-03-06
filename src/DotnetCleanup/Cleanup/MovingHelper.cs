﻿using System;
using System.IO;

using KeyLocks;

namespace DotnetCleanup.Cleanup
{
    internal class MovingHelper
    {
        private static readonly NameLock DirectoryLock = new NameLock();

        private readonly CommandContext _context;

        public MovingHelper(CommandContext context)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        public PathInfo Move(PathInfo cleanupPath)
        {
            if (_context.NoMove)
                return cleanupPath;

            var movePath = GetMovePath(cleanupPath);

            EnsureDirectory(movePath);

            if (cleanupPath.IsFile)
            {
                movePath = MoveFile(cleanupPath, movePath);
            }
            else
            {
                movePath = MoveDirectory(cleanupPath, movePath);
            }

            return new PathInfo(movePath);
        }

        private string GetMovePath(PathInfo cleanupPath)
        {
            var cleanupFolder =
                $"~dotnetcleanup-{_context.StartedAt:yyyyMMdd-HHmmss}";

            var movePath = Path.Combine(_context.TempPath, cleanupFolder);

            var movePathRoot = Path.GetPathRoot(movePath).ToLowerInvariant();

            var cleanupPathRoot =
                Path
                    .GetPathRoot(cleanupPath.Value)
                    .ToLowerInvariant();

            if (movePathRoot == cleanupPathRoot)
            {
                return movePath;
            }

            var parentCleanupPath =
                PathUtility.GetParentPath(cleanupPath.Value);

            return Path.Combine(parentCleanupPath, cleanupFolder);
        }

        private static string MoveDirectory(PathInfo cleanupPath, string movePath)
        {
            var directoryName = Path.GetFileName(cleanupPath.Value);

            var random = Path.GetRandomFileName();
            var randomDirectoryName = $"{directoryName}-{random}";

            movePath = Path.Combine(movePath, randomDirectoryName);

            Directory.Move(cleanupPath.Value, movePath);

            return movePath;
        }

        private string MoveFile(PathInfo cleanupPath, string movePath)
        {
            var filename = Path.GetFileName(cleanupPath.Value);

            movePath = Path.Combine(movePath, filename);

            File.Move(cleanupPath.Value, movePath);

            return movePath;
        }

        private static void EnsureDirectory(string path)
        {
            if (Directory.Exists(path))
                return;

            var key = path.ToLowerInvariant();

            DirectoryLock.RunWithLock(
                key,
                () =>
                {
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                });
        }
    }
}
