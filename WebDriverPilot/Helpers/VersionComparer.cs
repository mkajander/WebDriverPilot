using System.Collections.Generic;

namespace WebDriverPilot.Helpers
{
    public class VersionComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var xVersion = x.GetVersion();
            var yVersion = y.GetVersion();
            return xVersion.CompareTo(yVersion);
        }
    }
}