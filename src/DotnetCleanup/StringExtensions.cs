using System;

namespace DotnetCleanup
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string s, string value)
        {
            return s.Equals(value, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}