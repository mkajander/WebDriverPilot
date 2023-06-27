using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebDriverManager.Finders;
using WebDriverManager.Helpers;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
namespace WebDriverManager.Tests
{
    public class DriverDownloaderTests
    {
        private readonly ITestOutputHelper _output;

        public DriverDownloaderTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public async Task ShouldGetLatestChromeDriverVersion()
        {
            var driverDownloader = new DriverDownloader(overwriteExistingDriver:true);
            var version = await driverDownloader.GetLatestVersion(DriverType.Chrome);
            _output.WriteLine("Latest ChromeDriverVersion {0}", version);
            version.Should().NotBeNull();
        }
        
        [Fact]
        public async Task ShouldGetLatestEdgeDriverVersion()
        {
            var driverDownloader = new DriverDownloader(overwriteExistingDriver:true);
            var version = await driverDownloader.GetLatestVersion(DriverType.Edge);
            _output.WriteLine("Latest EdgeDriverVersion {0}", version);
             Assert.NotNull(version);
        }
        
        [Fact]
        public async Task ShouldDownloadLatestChromeDriverVersion()
        {
            var driverDownloader = new DriverDownloader(overwriteExistingDriver:true);
            var path = await driverDownloader.DownloadLatestVersion(DriverType.Chrome);
            _output.WriteLine("Downloaded and extracted latest ChromeDriver to {0}", path);
            Assert.NotNull(path);
        }
        
        [Fact]
        public async Task ShouldDownloadLatestEdgeDriverVersion()
        {
            var driverDownloader = new DriverDownloader(overwriteExistingDriver:true);
            var path = await driverDownloader.DownloadLatestVersion(DriverType.Edge);
            _output.WriteLine("Downloaded and extracted latest EdgeDriver to {0}", path);
            Assert.NotNull(path);
        }
        
        [Theory]
        [InlineData("102.0.5005.61")]
        public async Task ShouldDownloadSpecificChromeDriverVersion(string version)
        {
            var driverDownloader =new DriverDownloader(overwriteExistingDriver:true);
            var path = await driverDownloader.DownloadVersion(DriverType.Chrome, new Version(version));
            _output.WriteLine("Downloaded and extracted ChromeDriver version {0} to {1}", path, version);
            Assert.NotNull(path);
        }
        [Theory]
        [InlineData("100.0.1155.0")]
        public async Task ShouldDownloadSpecificEdgeDriverVersion(string version)
        {
            var driverDownloader = new DriverDownloader(overwriteExistingDriver:true);
            var path = await driverDownloader.DownloadVersion(DriverType.Edge, new Version(version));
            _output.WriteLine("Downloaded and extracted ChromeDriver version {0} to {1}", path, version);
            Assert.NotNull(path);
        }
        
        [Fact]
        public async Task ShouldKeepOnlyLatestThreeDriversByDefault()
        {
            var versions = new List<int> {100, 101, 102, 103, 104, 105};
            var driverDownloader = new DriverDownloader(overwriteExistingDriver:true);
            foreach (var version in versions)
            {
                await driverDownloader.DownloadVersion(DriverType.Chrome, new Version(version, 0));
            }
            var files = System.IO.Directory.GetFiles(driverDownloader.DriverFolder, "chromedriver*.exe");
            files.Length.Should().Be(3);
        }
    }
}