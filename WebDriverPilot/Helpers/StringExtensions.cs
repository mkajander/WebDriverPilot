using System;
using System.Text.RegularExpressions;

namespace WebDriverPilot.Helpers
{
    public static class StringExtensions
    {
        public static Version GetVersion(this string source)
        {
            // make sure that we only consider the actual file
            source = source.Substring(source.LastIndexOf("\\", StringComparison.Ordinal) + 1);
            var version = new Version(0, 0);
            // any combination of xx.xx.xx.xx where xx is a number, dont include the last . since that is the file extension
            var versionMatch = Regex.Match(source, @"(\d+\.){1,3}\d+");
            if (versionMatch.Success)
            {
                Version.TryParse(versionMatch.Value, out version);
            }
            
            return version;
        }
    }
}