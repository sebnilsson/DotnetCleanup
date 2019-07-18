using System;
using System.Diagnostics;
using System.IO;

namespace DotnetCleanup
{
    [DebuggerDisplay("{Value}")]
    public class PathInfo
    {
        private readonly Lazy<bool> _existsLazy;
        private readonly Lazy<string> _extensionLazy;
        private readonly Lazy<bool> _isFileLazy;

        public PathInfo(string path)
        {
            Value = path ?? throw new ArgumentNullException(nameof(path));

            _existsLazy = new Lazy<bool>(GetExists);
            _extensionLazy = new Lazy<string>(GetExtension);
            _isFileLazy = new Lazy<bool>(GetIsFile);
        }

        private bool GetExists()
        {
            return Directory.Exists(Value) || File.Exists(Value);
        }

        private string GetExtension()
        {
            return Path.GetExtension(Value);
        }

        private bool GetIsFile()
        {
            return
                Exists &&
                ((File.GetAttributes(Value) & FileAttributes.Directory)
                    != FileAttributes.Directory);
        }

        public bool Exists => _existsLazy.Value;

        public string Extension => _extensionLazy.Value;

        public bool IsFile => _isFileLazy.Value;

        public string Value { get; }
    }
}