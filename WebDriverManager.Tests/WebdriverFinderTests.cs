using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using WebDriverManager.Finders;
using Xunit;
using Xunit.Abstractions;

namespace WebDriverManager.Tests
{
    public class WebdriverFinderTests
    {
        private readonly ITestOutputHelper _output;

        public WebdriverFinderTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public async Task FinderShouldFindDriver()
        {
            // create test folder if it doesn't exist
            var testFolder = "Drivers";
            if (!System.IO.Directory.Exists(testFolder))
            {
                System.IO.Directory.CreateDirectory(testFolder);
            }
            var finder = new EdgeWebDriverFinder(NullLogger<EdgeWebDriverFinder>.Instance);
            var path = await finder.Configure(
                downloadDriver: true,
                driveFolder: testFolder
            ).FindAvailableDrivers().FindBrowserVersion().ReturnMatchedDriverPath();
            _output.WriteLine("Found matching driver at: " + path);
            Assert.NotNull(path);
        }
    }
}