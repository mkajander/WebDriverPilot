using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebDriverPilot.Helpers;

namespace WebDriverPilot.Finders
{
    public class DriverDownloader
    {
        private readonly int _numberOfDriversToKeep;
        private readonly bool _lookForBrowserVersion;

        private static readonly ReadOnlyDictionary<DriverType, string> LatestVersionUrls
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "https://chromedriver.storage.googleapis.com/LATEST_RELEASE" },
                    { DriverType.Edge, "https://msedgedriver.azureedge.net/LATEST_STABLE" }
                }
            );

        private static readonly ReadOnlyDictionary<DriverType, string> BlobRepositoryUrls
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "https://chromedriver.storage.googleapis.com" },
                    { DriverType.Edge, "https://msedgedriver.azureedge.net" }
                }
            );

        private static readonly ReadOnlyDictionary<DriverType, string> DownloadUrls
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "https://chromedriver.storage.googleapis.com/{0}/chromedriver_{1}.zip" },
                    {
                        DriverType.Edge,
                        "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/{0}/edgedriver_{1}.zip"
                    }
                }
            );

        private static readonly ReadOnlyDictionary<DriverType, string> DriverHashAlgorithm
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "NOT_AVAILABLE" },
                    { DriverType.Edge, "MD5" }
                }
            );

        private static readonly ReadOnlyDictionary<DriverType, string> DriverFileNameWithoutExtension
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "chromedriver" },
                    { DriverType.Edge, "msedgedriver" }
                }
            );


        const string DriverVersionRegex = @"([0-9]+(\.[0-9]+)+)";
        const string VersionMatchPattern = @"^\d+\.";

        const string ChromeDriverFileName = "chromedriver.zip";
        const string EdgeDriverFileName = "msedgedriver.zip";

        private static HttpClient Client = new HttpClient();

        public string DriverFolder { get; private set; }
        public bool OverwriteExistingDriver { get; private set; }

        public DriverDownloader(string driverFolder = null, bool overwriteExistingDriver = false,
            int numberOfDriversToKeep = 3, bool lookForBrowserVersion = true)
        {
            _numberOfDriversToKeep = numberOfDriversToKeep;
            _lookForBrowserVersion = lookForBrowserVersion;
            DriverFolder = driverFolder ?? FolderHelpers.GetProgramPath();
            OverwriteExistingDriver = overwriteExistingDriver;
            // Without this edgeDriver blobRepositoryUrl is 404 not found
            Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        }

        public async Task<bool> ValidateDriverHash(FileInfo driver, string version, DriverType driverType)
        {
            if (driverType == DriverType.Chrome)
            {
                // Why google? Why?
                return true;
            }

            var driverHash = driver.GetFileHash(DriverHashAlgorithm[driverType]);


            var response = await Client.GetAsync(BlobRepositoryUrls[driverType]);
            var blob = await response.Content.ReadAsStringAsync();

            // get the hash from the blob file (xml)
            // select <Blob> <Name>{version}/edgedriver_win64.zip</Name> <Properties>
            // hash is in <Content-MD5> tag in the <Properties> tag
            var has2 = XDocument.Parse(blob);
            var hash = XDocument.Parse(blob)
                .Descendants("Blob")
                .Where(x => x.Element("Name").Value == $"{version}/edgedriver_win64.zip")
                .Select(x => x.Element("Properties").Element("Content-MD5").Value)
                .FirstOrDefault();

            return driverHash.Equals(hash);
        }

        public async Task<Version> GetLatestVersion(DriverType driverType)
        {
            switch (driverType)
            {
                case DriverType.Chrome:
                    return await GetLatestChromeDriverVersion();
                case DriverType.Edge:
                    return await GetLatestEdgeDriverVersion();
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }


        private async Task<Version> GetLatestChromeDriverVersion()
        {
            var response = await Client.GetAsync(LatestVersionUrls[DriverType.Chrome]).ConfigureAwait(false);
            var blob = await response.Content.ReadAsStringAsync();
            var version = blob.GetVersion();
            return version;
        }

        private async Task<Version> GetLatestEdgeDriverVersion()
        {
            // unlike chrome edge driver latest version comes in a blob file containing the version
            var response = await Client.GetAsync(LatestVersionUrls[DriverType.Edge]).ConfigureAwait(false);
            // get the blob file
            var blob = await response.Content.ReadAsStringAsync();
            // get the version from the blob file
            var version = blob.GetVersion();
            return version;
        }

        public async Task<string> DownloadDriver(DriverType driverType, Version version)
        {
            var downloadUrl = string.Format(DownloadUrls[driverType], version, GetDriverArchitecture(driverType));
            var response = await Client.GetAsync(downloadUrl).ConfigureAwait(false);
            var zipPath = Path.Combine(DriverFolder, GetDriverFileName(driverType));
            using (var fileStream = File.Create(zipPath))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            // Google does not provide the driver version in the FileVersionInfo
            var driverPath = await UnzipDriver(driverType, zipPath, driverType == DriverType.Chrome ? version : null);
            // delete old drivers
            DeleteOldDrivers(driverType);
            return driverPath;
        }

        private void DeleteOldDrivers(DriverType driverType)
        {
            var driverFileName = DriverFileNameWithoutExtension[driverType];
            var driverFiles = Directory.GetFiles(DriverFolder, driverFileName + "*");
            if (driverFiles.Length > 1)
            {
                var orderedFiles = driverFiles.OrderByDescending(x => x, new VersionComparer());
                var filesToDelete = orderedFiles.Skip(_numberOfDriversToKeep);
                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                }
            }
        }



        private async Task<string> UnzipDriver(DriverType driverType, string pathToZip, Version version = null)
        {
            string driverPath = "";

            using (ZipArchive archive = ZipFile.OpenRead(pathToZip))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.Contains(".exe"))
                    {
                        driverPath = Path.Combine(DriverFolder, entry.Name);
                        entry.ExtractToFile(driverPath, true);
                        break;
                    }
                }
            }

            var file = new FileInfo(driverPath);
            version = version ?? file.FullName.GetFileVersionInfo().FileVersion.GetVersion();
            // Content-MD5 is actually the MD5 hash of the zip file and not the content of the zip file
            if (!await ValidateDriverHash(new FileInfo(pathToZip), version.ToString(), driverType))
            {
                // delete the driver
                File.Delete(pathToZip);
                throw new Exception("Driver hash validation failed");
            }

            File.Delete(pathToZip);

            if (file.Directory == null)
            {
                throw new Exception("Could not get directory of driver");
            }

            var newName = Path.Combine(file.Directory.FullName,
                $"{DriverFileNameWithoutExtension[driverType]}-{version}{file.Extension}");
            return new FileInfo(driverPath).Rename(newName, OverwriteExistingDriver).FullName;
        }

        public async Task<string> DownloadLatestVersion(DriverType driverType)
        {
            var version = await GetLatestVersion(driverType);
            return await DownloadDriver(driverType, version);
        }

        public string GetDriverArchitecture(DriverType driverType)
        {
            // Todo: Implement this in an actually sensible manner so it works for all platforms if someone wants to use this for something other than windows
            // Not that anyone is going to use this for anything other than windows and maybe linux64
            switch (driverType)
            {
                case DriverType.Chrome:
                    return "win32";
                case DriverType.Edge:
                    return "win64";
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }

        public string GetDriverFileName(DriverType driverType)
        {
            switch (driverType)
            {
                case DriverType.Chrome:
                    return ChromeDriverFileName;
                case DriverType.Edge:
                    return EdgeDriverFileName;
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }

        public async Task<string> DownloadVersion(DriverType driverType, Version browserVersion)
        {
            var driverVersion = await GetMatchingDriverVersion(driverType, browserVersion);

            return await DownloadDriver(driverType, driverVersion.GetVersion());
        }

        private async Task<string> GetMatchingDriverVersion(DriverType driverType, Version browserVersion)
        {
            switch (driverType)
            {
                case DriverType.Chrome:
                    return await GetSpecificChromeDriverVersion(browserVersion);
                case DriverType.Edge:
                    return await GetSpecificEdgeDriverVersion(browserVersion);
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }

        private async Task<string> GetSpecificEdgeDriverVersion(Version browserVersion)
        {
            //regex to trim the version number
            var version = browserVersion.Major;
            var response = await Client.GetAsync(BlobRepositoryUrls[DriverType.Edge]).ConfigureAwait(false);
            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);
            var availableDrivers = doc.Root.DescendantNodes().OfType<XElement>().Where(x => x.Name.LocalName == "Blob");
            foreach (var availableDriver in availableDrivers)
            {
                string name = availableDriver.Element("Name").Value;
                if (name.StartsWith(version.ToString()) && name.EndsWith("win32.zip"))
                {
                    return availableDriver.Value.Split('/')[0];
                }
            }

            throw new Exception("Could not find a driver for version " + version);
        }

        private async Task<string> GetSpecificChromeDriverVersion(Version browserVersion)
        {
            //regex to trim the version number
            var version = browserVersion.Major;
            var response = await Client.GetAsync(BlobRepositoryUrls[DriverType.Chrome]).ConfigureAwait(false);
            var xml = await response.Content.ReadAsStringAsync();
            // Parse the xml
            var doc = XDocument.Parse(xml);
            var availableDrivers = doc.Root?.DescendantNodes().OfType<XElement>().Where(x => x.Name.LocalName == "Key")
                .OrderByDescending(x => x.Value) ?? Enumerable.Empty<XElement>();
            foreach (var availableDriver in availableDrivers)
            {
                if (availableDriver.Value.StartsWith(version.ToString()) && availableDriver.Value.EndsWith("win32.zip"))
                {
                    return availableDriver.Value.Split('/')[0];
                }
            }

            throw new Exception("Could not find a driver for version " + version);
        }
    }
}