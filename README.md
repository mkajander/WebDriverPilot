# WebDriverManager
Library for automatically downloading EdgeWebdriver and ChromeWebdriver based on installed browser versions. A bit work in progress but does work for test cases.

WIP

## Usage

```csharp
var finder = new EdgeWebDriverFinder(NullLogger<EdgeWebDriverFinder>.Instance);
var path = await finder.Configure(downloadDriver: true).FindAvailableDrivers().FindBrowserVersion().ReturnMatchedDriverPath();
```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)
