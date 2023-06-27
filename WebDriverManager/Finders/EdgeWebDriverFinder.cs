using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebDriverManager.Exceptions;
using WebDriverManager.Helpers;

namespace WebDriverManager.Finders
{
    public interface IWebDriverFinder: IConfigure, IFindAvailableDrivers, IFindBrowserVersion
    {
    }
    public interface IConfigure
    {
        IFindAvailableDrivers FindAvailableDrivers();
    }

    public interface IFindAvailableDrivers
    {
        IFindBrowserVersion FindBrowserVersion();
    }

    public interface IFindBrowserVersion
    {
        Task<string> ReturnMatchedDriverPath();
    }


    public class EdgeWebDriverFinder : IWebDriverFinder
    {
        private readonly ILogger _logger;

        public EdgeWebDriverFinder(ILogger<EdgeWebDriverFinder> logger)
        {
            _logger = logger;
            // Add exception handling here for when the driver is not found
            
        }

        public static DriverType DriverType = DriverType.Edge;
        public Dictionary<string, string> AvailableDrivers { get; private set; }
        public Version EdgeVersion { get; private set; }

        public string EdgePath { get; private set; } = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

        public string DriveFolder { get; private set; }
            
        public bool ShouldDownloadDriver { get; private set; } = false;

        public IFindAvailableDrivers FindAvailableDrivers()
        {
            var drivers = new Dictionary<string, string>();
            var partialName = "msedgedriver";
            var hdDirectoryInWhichToSearch = new DirectoryInfo(DriveFolder);
            var filesInDir = hdDirectoryInWhichToSearch.GetFiles("*" + partialName + "*").ToArray();

            foreach (var foundFile in filesInDir)
            {
                var fullName = foundFile.FullName;
                var hasversionInName = foundFile.Name.Contains('-');
                var version = fullName.GetFileVersionInfo().FileVersion;
                if (hasversionInName)
                {
                    drivers.Add(version, fullName);
                    continue;
                }

                var newName = Path.Combine(foundFile.Directory.FullName,
                    $"msedgedriver-{version}{foundFile.Extension}");
                if (File.Exists(newName))
                {
                    File.Delete(newName); //if this file exists then delete it
                    drivers.Remove(version);
                }

                File.Move(foundFile.FullName, newName);
                drivers.Add(version, newName);
            }

            _logger.LogInformation("Found drivers", drivers);

            AvailableDrivers = drivers;
            return this;
        }

        public IFindBrowserVersion FindBrowserVersion()
        {
            try
            {
                EdgeVersion = EdgePath.GetFileVersionInfo().FileVersion.GetVersion();
                _logger.LogInformation($"Edge Version {EdgeVersion}");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError("Edge Version not Found.", ex);
            }

            return this;
        }

        public async Task<string> ReturnMatchedDriverPath()
        {
            
            foreach (var availableDriver in AvailableDrivers)
            {
                Version availableDriverVersion = availableDriver.Key.GetVersion();
                if (availableDriverVersion == EdgeVersion)
                {
                    _logger.LogInformation("Returning driver", availableDriver);
                    return availableDriver.Value;
                }
            }


            if (ShouldDownloadDriver)
            {
                _logger.LogInformation("Downloading driver");
                return await DownloadDriver();
            }

            throw new WebDriverFinderException("EdgeDriver not found or version not available", AvailableDrivers,
                EdgeVersion.ToString());
        }

        public IConfigure Configure(string edgePath = null, string driveFolder = null, bool downloadDriver = false)
        {
            // get the directory path to current assembly
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            EdgePath = edgePath ?? EdgePath;
            ShouldDownloadDriver = downloadDriver;
            DriveFolder = driveFolder ?? assemblyPath;
            return this;
        }

        private async Task<string> DownloadDriver()
        {
            var driverDownloader = new DriverDownloader(driverFolder: DriveFolder,overwriteExistingDriver:true);
            return await driverDownloader.DownloadVersion(DriverType, EdgeVersion);
        }
    }
}