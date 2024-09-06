# FluentConnectionStringBuilder

The `FluentConnectionStringBuilder` is a fluent API utility designed to construct database connection strings in a type-safe, readable, and composable manner. It abstracts the complexity of manual string concatenation and key-value pair management by providing dedicated methods for common connection parameters such as host, database, credentials, and security settings. The builder pattern ensures that configuration steps can be chained logically, culminating in a final validated connection string via the `Build` property.

## API

### `public static FluentConnectionStringBuilder For`
Initiates the building process for a specific database provider or context. This static entry point returns a new instance of the builder to begin configuration.
*   **Parameters**: None (context is typically inferred or set via subsequent calls depending on implementation specifics).
*   **Returns**: A new instance of `FluentConnectionStringBuilder`.
*   **Throws**: None.

### `public FluentConnectionStringBuilder WithHost`
Configures the server address or hostname for the database connection.
*   **Parameters**: `string host` – The network address or name of the database server.
*   **Returns**: The current `FluentConnectionStringBuilder` instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if the host is null; `ArgumentException` if the host format is invalid.

### `public FluentConnectionStringBuilder WithDatabase`
Sets the target database name or catalog to connect to upon establishment.
*   **Parameters**: `string database` – The name of the specific database.
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: `ArgumentNullException` if the database name is null or empty.

### `public FluentConnectionStringBuilder WithCredentials`
Configures explicit authentication using a username and password.
*   **Parameters**: `string username`, `string password` – The credentials for authentication.
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: `ArgumentNullException` if username or password is null; `InvalidOperationException` if called after `WithIntegratedSecurity`.

### `public FluentConnectionStringBuilder WithIntegratedSecurity`
Enables Windows Authentication (Integrated Security) instead of SQL-based credentials.
*   **Parameters**: None.
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: `InvalidOperationException` if called after `WithCredentials`.

### `public FluentConnectionStringBuilder WithSsl`
Enforces SSL/TLS encryption for the connection channel.
*   **Parameters**: None (or optional boolean flag depending on overload, defaults to true).
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: None.

### `public FluentConnectionStringBuilder WithPooling`
Configures connection pooling behavior, enabling or disabling the reuse of physical connections.
*   **Parameters**: `bool enabled` – True to enable pooling; false to disable.
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: None.

### `public FluentConnectionStringBuilder WithTimeout`
Sets the time limit (in seconds) to wait for a connection to open before terminating the attempt.
*   **Parameters**: `int seconds` – The timeout duration.
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: `ArgumentOutOfRangeException` if seconds is less than or equal to zero.

### `public FluentConnectionStringBuilder WithOption`
Adds a custom key-value pair to the connection string for provider-specific settings not covered by dedicated methods.
*   **Parameters**: `string key`, `string value` – The custom option name and its value.
*   **Returns**: The current `FluentConnectionStringBuilder` instance.
*   **Throws**: `ArgumentNullException` if key is null; `ArgumentException` if the key is reserved or already defined by a strongly-typed method.

### `public string Build`
Generates the final formatted connection string based on the accumulated configuration.
*   **Parameters**: None (property accessor).
*   **Returns**: A fully formed `string` representing the connection string.
*   **Throws**: `InvalidOperationException` if required fields (such as Host) have not been configured.

## Usage

### Basic SQL Server Configuration
The following example demonstrates constructing a standard connection string with explicit credentials, a specific timeout, and SSL enabled.

```csharp
var connectionString = FluentConnectionStringBuilder.For("sqlserver")
    .WithHost("sql-prod-01.internal")
    .WithDatabase("InventoryDb")
    .WithCredentials("app_user", "SecurePassword123!")
    .WithSsl()
    .WithTimeout(30)
    .WithPooling(1, 100)
    .Build();

// Output: "Server=sql-prod-01.internal;Database=InventoryDb;User Id=app_user;Password=SecurePassword123!;Encrypt=True;TrustServerCertificate=False;Pooling=True;Min Pool Size=1;Max Pool Size=100;Connection Timeout=30"
```

### Windows Authentication with Custom Options
This example illustrates using Integrated Security and injecting a provider-specific option that lacks a dedicated fluent method.

```csharp
var connectionString = FluentConnectionStringBuilder.For
    .WithHost("local-db-cluster")
    .WithDatabase("Analytics")
    .WithIntegratedSecurity()
    .WithOption("ApplicationIntent", "ReadOnly")
    .WithOption("MultiSubnetFailover", "True")
    .Build;

// Output: "Server=local-db-cluster;Database=Analytics;Integrated Security=True;ApplicationIntent=ReadOnly;MultiSubnetFailover=True;"
```

## Notes

*   **Mutability and Chaining**: The builder methods return the same instance (`this`), allowing for fluent chaining. However, the internal state is mutable until `Build` is called. Do not share a single builder instance across multiple threads if configurations differ, as race conditions may corrupt the internal state before building.
*   **Authentication Conflicts**: Calling `WithCredentials` and `WithIntegratedSecurity` on the same instance will result in an `InvalidOperationException`. The last call in the chain does not automatically overwrite the previous; instead, the conflict is detected and thrown to prevent ambiguous connection states.
*   **Required Fields**: The `Build` property performs a validation check. Accessing it without setting at least the `Host` (via `WithHost`) will throw an `InvalidOperationException`.
*   **Custom Options**: The `WithOption` method allows extensibility but does not perform type validation on the value. It is the caller's responsibility to ensure the value format matches the underlying database provider's expectations. Reserved keys used by strongly-typed methods (e.g., "Server", "Database") should not be passed to `WithOption` to avoid duplication errors.
*   **Thread Safety**: The `FluentConnectionStringBuilder` class is not thread-safe. While the static `For` method is safe to call concurrently, individual builder instances must be confined to a single thread or execution context during configuration.
