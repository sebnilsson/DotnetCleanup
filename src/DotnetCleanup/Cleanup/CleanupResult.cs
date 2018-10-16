using System;
using System.Collections.Generic;

namespace DotnetCleanup.Cleanup
{
    public class CleanupResult
    {
        public ICollection<Success> Successes { get; } = new List<Success>();

        public ICollection<Error> Errors { get; } = new List<Error>();

        public void AddSuccess(PathInfo cleanupPath, PathInfo movePath)
        {
            lock (Successes)
            {
                Successes.Add(new Success(cleanupPath, movePath));
            }
        }

        public void AddError(PathInfo cleanupPath, Exception exception)
        {
            lock (Errors)
            {
                Errors.Add(new Error(cleanupPath, exception));
            }
        }

        public class Success
        {
            public Success(PathInfo cleanupPath, PathInfo movePath)
            {
                CleanupPath = cleanupPath;
                MovePath = movePath;
            }

            public PathInfo CleanupPath { get; }

            public PathInfo MovePath { get; }
        }

        public class Error
        {
            public Error(PathInfo cleanupPath, Exception exception)
            {
                CleanupPath = cleanupPath;
                Exception = exception;
            }

            public PathInfo CleanupPath { get; }

            public Exception Exception { get; }
        }
    }
}