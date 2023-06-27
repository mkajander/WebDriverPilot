using System;
using System.Collections.Generic;

namespace WebDriverPilot.Exceptions
{
    [Serializable]
    public class WebDriverFinderException : Exception
    {
        public Dictionary<string,string> DriversAvailable { get; }
        public string EdgeVersion { get; }
        public WebDriverFinderException() { }
        public WebDriverFinderException(string message)
            : base(message) { }
        public WebDriverFinderException(string message, Exception inner)
            : base(message, inner) { }
        public WebDriverFinderException(string message, Dictionary<string, string> driversAvailable, string edgeVersion) : this(message)
        {
            DriversAvailable = driversAvailable;
            EdgeVersion = edgeVersion;
        }
    }
}