# conn-string-doctor

Diagnoses connection strings: parsing, reachability, TLS, pooling, timeouts, and comparison.

> v0.1 in progress.

## FluentConnectionStringBuilder

The `FluentConnectionStringBuilder` class provides a fluent interface for constructing provider-specific connection strings. 
It allows you to build connection strings for various database providers using method chaining, with proper escaping and provider-specific formatting.

Here's an example usage:

## ConnectionStringInfo

The `ConnectionStringInfo` class represents parsed connection string details, including provider type, server, port, database, user credentials, and additional properties. It is used by conversion and validation components to process connection strings.

## ConnectionStringConverterValidation

The `ConnectionStringConverterValidation` class provides extension methods for validating `ConnectionStringConverter.ConnectionStringInfo` and `ConversionResult` instances. It offers three validation modes: returning a list of problems, returning a boolean, or throwing an exception when invalid. This enables fluent validation patterns in your code.

**Example usage**

```csharp
using ConnStringDoctor;

// Parse a connection string
var parsed = ConnectionStringConverter.Parse("Server=localhost;Database=test;");

// Validate and get problems
var problems = parsed.Validate();
if (problems.Count > 0)
{
    Console.WriteLine("Validation failed:");
    foreach (var problem in problems)
    {
        Console.WriteLine($"- {problem}");
    }
}

// Quick validation check
if (parsed.IsValid())
{
    Console.WriteLine("Connection string is valid!");
}

// Throw if invalid
parsed.EnsureValid();

// Validate ConversionResult
var result = ConnectionStringConverter.Convert(parsed);
result.EnsureValid();
```

## ConnectionStringConverterExtensions

`ConnectionStringConverterExtensions` adds a collection of handy extension methods for `ConnectionStringConverter`, `ConversionResult`, and `ConnectionStringInfo`.  
These helpers make it easier to perform conversions, inspect results, and retrieve common parts of a parsed connection string without dealing with the underlying dictionaries directly.

**Example usage**


## Connection String Comparison

The `compare` command allows you to compare two connection strings key-by-key, showing:
- Keys that are only in the first connection string (⊖)
- Keys that are only in the second connection string (⊕)
- Keys with different values (✗)
- Matching keys (✓ when using --show-all)

Credentials are redacted by default for security.

**Example usage**

```bash
# Compare two connection strings
dotnet run -- compare \
  --connection-string-a "Server=localhost;Database=test;User Id=admin;Password=secret123;Timeout=30" \
  --connection-string-b "Server=localhost;Database=test;User Id=admin;Password=secret456;Timeout=60"

# Output shows: ✗ timeout
#                A: 30
#                B: 60

# Compare with all keys shown
dotnet run -- compare \
  --connection-string-a "Server=localhost;Database=test;User Id=admin;Password=secret123;Timeout=30;Encrypt=true" \
  --connection-string-b "Server=localhost;Database=test;User Id=admin;Password=secret456;Timeout=30" \
  --show-all

# Output shows all keys with differences marked
```
