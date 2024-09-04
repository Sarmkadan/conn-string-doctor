# ConnectionStringInfo

The `ConnectionStringInfo` class provides a strongly-typed, structured representation of a database connection string. It allows developers to parse, inspect, and manipulate individual connection parameters—such as the server address, port, authentication credentials, and provider-specific properties—without needing to manage the complexities of provider-specific connection string formatting or parsing syntax.

## API

### Properties

*   **`public DbProvider Provider`**
    Gets or sets the database provider type associated with the connection.

*   **`public string? Server`**
    Gets or sets the server address (hostname or IP address). May be `null` if not defined.

*   **`public int? Port`**
    Gets or sets the TCP port number. Returns `null` if the port is not explicitly specified.

*   **`public string? Database`**
    Gets or sets the name of the database. May be `null` if not defined.

*   **`public string? User`**
    Gets or sets the username used for authentication. May be `null` if the connection uses integrated security or no username is required.

*   **`public string? Password`**
    Gets or sets the password used for authentication. May be `null` if not defined.

*   **`public Dictionary<string, string> Properties`**
    A collection of additional key-value pairs representing provider-specific connection string configuration (e.g., `Timeout`, `Encrypt`).

### Methods

*   **`public override string ToString()`**
    Returns a string representation of the `ConnectionStringInfo` instance, typically formatted as a valid connection string suitable for consumption by a database driver.

## Usage

### Example 1: Inspecting a Parsed Connection String
This example demonstrates how to access specific properties after parsing a raw connection string.

```csharp
// Assume connInfo was parsed from a raw string
ConnectionStringInfo connInfo = ConnectionStringParser.Parse("Server=myServer;Database=myDb;User=admin;Port=5432;");

Console.WriteLine($"Provider: {connInfo.Provider}");
Console.WriteLine($"Server: {connInfo.Server ?? "N/A"}");
Console.WriteLine($"Database: {connInfo.Database ?? "N/A"}");

if (connInfo.Port.HasValue)
{
    Console.WriteLine($"Port: {connInfo.Port.Value}");
}
```

### Example 2: Modifying Connection Properties
This example demonstrates how to create or modify `ConnectionStringInfo` and add provider-specific properties.

```csharp
var connInfo = new ConnectionStringInfo
{
    Provider = DbProvider.PostgreSQL,
    Server = "localhost",
    Database = "production_db",
    User = "db_user",
    Password = "secure_password"
};

// Add provider-specific configuration
connInfo.Properties["CommandTimeout"] = "30";
connInfo.Properties["TrustServerCertificate"] = "true";

// Retrieve the formatted connection string
string finalConnectionString = connInfo.ToString();
```

## Notes

*   **Nullability:** The `Server`, `Port`, `Database`, `User`, and `Password` properties are nullable. It is the responsibility of the consumer to verify these values (e.g., checking `HasValue` for `int?`) before usage to avoid unexpected behavior or exceptions in dependent components.
*   **Properties Dictionary:** The `Properties` dictionary is initialized upon instantiation. It does not perform validation on the keys or values provided; it serves as a storage mechanism for arbitrary string-based configurations.
*   **Thread Safety:** The `ConnectionStringInfo` class and its `Properties` dictionary are not thread-safe. If an instance is intended to be shared across multiple threads where modifications might occur, external synchronization mechanisms (e.g., `lock`) should be employed to ensure data consistency.
*   **`ToString()` Behavior:** The implementation of `ToString()` relies on the current state of the properties. If essential connection parameters are missing, the resulting string may not be a valid connection string for the specified `Provider`.
