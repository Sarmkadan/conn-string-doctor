# ConnectionStringConverterExtensions

Utility class providing extension methods for converting, parsing, and inspecting connection strings and their components. These methods facilitate safe handling of connection string values, including validation, extraction of database and server information, and detection of potential issues such as unmapped keys or conversion warnings.

## API

### `TryConvert`

Converts a raw connection string into a standardized format, optionally applying transformations or validation rules.

```csharp
public static bool TryConvert(string rawConnectionString, [MaybeNullWhen(false)] out string convertedConnectionString)
```

- **rawConnectionString**: The original connection string to convert.
- **convertedConnectionString**: When this method returns, contains the converted connection string if conversion succeeded; otherwise, `null`.
- **Returns**: `true` if conversion succeeded; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `rawConnectionString` is `null`.

---

### `GetValue`

Extracts the value associated with a specific key from a connection string.

```csharp
public static string? GetValue(string connectionString, string key)
```

- **connectionString**: The connection string to inspect.
- **key**: The key whose value should be retrieved.
- **Returns**: The value associated with `key`, or `null` if the key is not present or the connection string is invalid.
- **Throws**: `ArgumentNullException` if `connectionString` or `key` is `null`.

---

### `HasWarnings`

Indicates whether the most recent parsing operation produced any warnings.

```csharp
public static bool HasWarnings()
```

- **Returns**: `true` if warnings were generated during the last parsing or conversion operation; otherwise, `false`.

---

### `HasUnmappedKeys`

Indicates whether the most recent parsing operation encountered keys that were not recognized or mapped.

```csharp
public static bool HasUnmappedKeys()
```

- **Returns**: `true` if unmapped keys were found during the last parsing or conversion operation; otherwise, `false`.

---

### `FirstWarning`

Retrieves the first warning message generated during the most recent parsing or conversion operation.

```csharp
public static string? FirstWarning()
```

- **Returns**: The first warning message, or `null` if no warnings were generated.

---

### `FirstUnmappedKey`

Retrieves the first key that was not recognized or mapped during the most recent parsing or conversion operation.

```csharp
public static string? FirstUnmappedKey()
```

- **Returns**: The first unmapped key, or `null` if no unmapped keys were found.

---
### `ParseAndConvert`

Parses a raw connection string, applies internal conversion logic, and returns a structured result containing both the converted string and diagnostic information.

```csharp
public static ConversionResult ParseAndConvert(string rawConnectionString)
```

- **rawConnectionString**: The original connection string to parse and convert.
- **Returns**: A `ConversionResult` containing the converted connection string, warnings, and unmapped keys.
- **Throws**: `ArgumentNullException` if `rawConnectionString` is `null`.

---

### `GetDatabaseName`

Extracts the database name from a connection string, if present.

```csharp
public static string? GetDatabaseName(string connectionString)
```

- **connectionString**: The connection string to inspect.
- **Returns**: The database name, or `null` if not found or the connection string is invalid.
- **Throws**: `ArgumentNullException` if `connectionString` is `null`.

---
### `GetServerName`

Extracts the server name from a connection string, if present.

```csharp
public static string? GetServerName(string connectionString)
```

- **connectionString**: The connection string to inspect.
- **Returns**: The server name, or `null` if not found or the connection string is invalid.
- **Throws**: `ArgumentNullException` if `connectionString` is `null`.

---
### `GetPort`

Extracts the port number from a connection string, if present and valid.

```csharp
public static int? GetPort(string connectionString)
```

- **connectionString**: The connection string to inspect.
- **Returns**: The port number, or `null` if not found, invalid, or the connection string is malformed.
- **Throws**: `ArgumentNullException` if `connectionString` is `null`.

## Usage

### Example 1: Safe conversion and inspection

```csharp
using var connString = "Server=db.example.com;Port=5432;Database=mydb;User Id=admin;Password=secret;";

if (ConnectionStringConverterExtensions.TryConvert(connString, out var converted))
{
    Console.WriteLine($"Converted: {converted}");

    if (ConnectionStringConverterExtensions.HasWarnings())
    {
        Console.WriteLine($"Warning: {ConnectionStringConverterExtensions.FirstWarning()}");
    }

    var dbName = ConnectionStringConverterExtensions.GetDatabaseName(converted);
    Console.WriteLine($"Database: {dbName}");
}
else
{
    Console.WriteLine("Conversion failed.");
}
```

### Example 2: Full diagnostic parsing

```csharp
var raw = "Server=localhost;Database=test;UnrecognizedKey=value;";

var result = ConnectionStringConverterExtensions.ParseAndConvert(raw);

Console.WriteLine($"Converted: {result.ConvertedConnectionString}");

if (result.Warnings.Any())
{
    Console.WriteLine("Warnings:");
    foreach (var w in result.Warnings)
    {
        Console.WriteLine($"- {w}");
    }
}

if (result.UnmappedKeys.Any())
{
    Console.WriteLine("Unmapped keys:");
    foreach (var k in result.UnmappedKeys)
    {
        Console.WriteLine($"- {k}");
    }
}
```

## Notes

- **Thread Safety**: All methods are thread-safe and do not maintain mutable state. They operate on input parameters and return results without side effects.
- **State Isolation**: Methods like `HasWarnings`, `HasUnmappedKeys`, `FirstWarning`, and `FirstUnmappedKey` reflect the state of the most recent call to `ParseAndConvert` within the current logical operation. This state is not persisted across unrelated operations or threads.
- **Null Handling**: Methods throw `ArgumentNullException` when required string parameters are `null`, but return `null` when keys are missing or values are absent in the connection string.
- **Port Parsing**: `GetPort` returns `null` for non-numeric or out-of-range port values; it does not throw for malformed values.
- **Conversion Behavior**: `TryConvert` and `ParseAndConvert` are designed to be resilient and will not throw on malformed input, instead returning `false` or a result with warnings.
