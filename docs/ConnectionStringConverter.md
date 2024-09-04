# ConnectionStringConverter
The `ConnectionStringConverter` type is designed to parse, analyze, and convert connection strings into a standardized format. It provides a set of properties and methods that allow developers to inspect the original connection string, identify unmapped keys, and retrieve warnings about potential issues. This type is part of the `conn-string-doctor` project, which aims to simplify the process of working with connection strings in various applications.

## API
* `public string Provider`: Gets the provider associated with the connection string.
* `public IReadOnlyDictionary<string, string> OriginalParts`: Gets a dictionary containing the original parts of the connection string.
* `public static ConnectionStringInfo Parse(string connectionString)`: Parses a connection string into a `ConnectionStringInfo` object. This method takes a connection string as input and returns a `ConnectionStringInfo` object. It may throw exceptions if the input string is malformed or cannot be parsed.
* `public ConversionResult Convert()`: Converts the connection string into a standardized format. This method returns a `ConversionResult` object, which contains information about the conversion process. It may throw exceptions if the conversion fails.
* `public string ConnectionString`: Gets the converted connection string.
* `public IReadOnlyList<string> UnmappedKeys`: Gets a list of keys that were not mapped during the conversion process.
* `public IReadOnlyList<string> Warnings`: Gets a list of warnings about potential issues with the connection string.
* `public ConversionResult`: This property seems to be a duplicate of the `Convert` method and its purpose is unclear. It is recommended to use the `Convert` method instead.

## Usage
The following examples demonstrate how to use the `ConnectionStringConverter` type:
```csharp
// Example 1: Parsing a connection string
var converter = new ConnectionStringConverter("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
var connectionStringInfo = ConnectionStringConverter.Parse(converter.ConnectionString);
Console.WriteLine(connectionStringInfo.Provider);

// Example 2: Converting a connection string
var converter2 = new ConnectionStringConverter("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
var conversionResult = converter2.Convert();
Console.WriteLine(conversionResult.ConnectionString);
```

## Notes
When using the `ConnectionStringConverter` type, keep in mind the following edge cases:
* The `Parse` method may throw exceptions if the input string is malformed or cannot be parsed. It is recommended to handle these exceptions accordingly.
* The `Convert` method may return a `ConversionResult` object with warnings about potential issues with the connection string. It is recommended to inspect these warnings and take necessary actions.
* The `UnmappedKeys` and `Warnings` properties may contain empty lists if no unmapped keys or warnings were found during the conversion process.
* The `ConnectionStringConverter` type is not thread-safe. If multiple threads need to access the same instance, it is recommended to synchronize access using locks or other synchronization mechanisms.
