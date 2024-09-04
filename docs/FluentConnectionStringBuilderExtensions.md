# FluentConnectionStringBuilderExtensions

Provides a set of fluent extension methods for configuring a `FluentConnectionStringBuilder` instance. These methods enable concise, readable construction of connection strings by chaining calls that set common options such as timeout, pool size, additional settings, and database name extraction from a URI.

## API

### WithOptions
```csharp
public static FluentConnectionStringBuilder WithOptions(
    this FluentConnectionStringBuilder builder,
    Action<ConnectionStringOptions> configure)
```
- **Purpose**: Applies additional connection‑string options via a configuration delegate.
- **Parameters**:
  - `builder`: The `FluentConnectionStringBuilder` instance to extend.
  - `configure`: A delegate that receives a `ConnectionStringOptions` object to set custom key/value pairs.
- **Return value**: The same `builder` instance, allowing further chaining.
- **Exceptions**:
  - `ArgumentNullException` if `builder` is `null`.
  - `ArgumentNullException` if `configure` is `null`.

### WithTimeout
```csharp
public static FluentConnectionStringBuilder WithTimeout(
    this FluentConnectionStringBuilder builder,
    int seconds)
```
- **Purpose**: Sets the connection timeout value (in seconds) for the underlying connection string.
- **Parameters**:
  - `builder`: The `FluentConnectionStringBuilder` instance to extend.
  - `seconds`: Timeout duration; must be a non‑negative integer.
- **Return value**: The same `builder` instance.
- **Exceptions**:
  - `ArgumentNullException` if `builder` is `null`.
  - `ArgumentOutOfRangeException` if `seconds` is less than zero.

### WithPoolSize
```csharp
public static FluentConnectionStringBuilder WithPoolSize(
    this FluentConnectionStringBuilder builder,
    int maxPoolSize)
```
- **Purpose**: Configures the maximum connection pool size.
- **Parameters**:
  - `builder`: The `FluentConnectionStringBuilder` instance to extend.
  - `maxPoolSize`: Desired maximum pool size; must be greater than zero.
- **Return value**: The same `builder` instance.
- **Exceptions**:
  - `ArgumentNullException` if `builder` is `null`.
  - `ArgumentOutOfRangeException` if `maxPoolSize` is less than or equal to zero.

### WithDatabaseFromUri
```csharp
public static FluentConnectionStringBuilder WithDatabaseFromUri(
    this FluentConnectionStringBuilder builder,
    Uri uri)
```
- **Purpose**: Extracts the database name from the supplied `Uri` and sets it as the `Database` (or `Initial Catalog`) part of the connection string.
- **Parameters**:
  - `builder`: The `FluentConnectionStringBuilder` instance to extend.
  - `uri`: A URI whose path segment containing the database name (e.g., `https://example.com/path/to/db`).
- **Return value**: The same `builder` instance.
- **Exceptions**:
  - `ArgumentNullException` if `builder` is `null`.
  - `ArgumentNullException` if `uri` is `null`.
  - `UriFormatException` if `uri` is not a valid absolute URI.
  - `InvalidOperationException` if the URI does not contain a segment that can be interpreted as a database name.

## Usage

```csharp
var builder = new FluentConnectionStringBuilder()
    .WithTimeout(30)
    .WithPoolSize(100)
    .WithOptions(opts => opts.Add("Encrypt", "true"))
    .WithDatabaseFromUri(new Uri("https://myaccount.sql.azure.com/dbname"));

string connectionString = builder.ToString();
// Result: "Timeout=30;Max Pool Size=100;Encrypt=true;Database=dbname;..."
```

```csharp
// Building a connection string for a local SQLite file
var sqliteBuilder = new FluentConnectionStringBuilder()
    .WithOptions(opts => opts.Add("Data Source", "C:\\data\\mydb.sqlite"))
    .WithTimeout(15);

string sqliteConn = sqliteBuilder.ToString();
// Result: "Data Source=C:\data\mydb.sqlite;Timeout=15;"
```

## Notes

- All extension methods return the same `FluentConnectionStringBuilder` instance, making them safe to chain in any order.
- The methods do not modify any static state; they operate solely on the provided instance, so they are thread‑safe with respect to concurrent calls on different builder instances.
- If a method is called multiple times on the same builder, later calls overwrite earlier values for the same property (e.g., calling `WithTimeout` twice will retain only the last timeout value).
- Passing `null` for the builder argument will always result in an `ArgumentNullException`; the methods do not provide a default builder.
- The `WithDatabaseFromUri` method expects the database name to be present as a path segment; query strings, fragments, or user‑info components are ignored for this purpose. If the URI lacks a suitable segment, an `InvalidOperationException` is thrown.
