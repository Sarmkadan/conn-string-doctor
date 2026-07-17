# ConnectionStringConverterValidation

Provides utility methods for validating and ensuring the correctness of connection strings, including type conversion and structural validation.

## API

### `public static IReadOnlyList<string> Validate(string connectionString)`

Validates the provided connection string and returns a list of validation messages. An empty list indicates the connection string is valid.

- **Parameters**
  - `connectionString` – The connection string to validate.
- **Returns**
  - `IReadOnlyList<string>` – A list of error messages; empty if valid.
- **Exceptions**
  - `ArgumentNullException` – Thrown if `connectionString` is `null`.

### `public static bool IsValid(string connectionString)`

Determines whether the provided connection string is valid without returning detailed messages.

- **Parameters**
  - `connectionString` – The connection string to check.
- **Returns**
  - `bool` – `true` if the connection string is valid; otherwise, `false`.
- **Exceptions**
  - `ArgumentNullException` – Thrown if `connectionString` is `null`.

### `public static void EnsureValid(string connectionString)`

Ensures the provided connection string is valid, throwing an exception if it is not.

- **Parameters**
  - `connectionString` – The connection string to validate.
- **Exceptions**
  - `ArgumentNullException` – Thrown if `connectionString` is `null`.
  - `InvalidOperationException` – Thrown if the connection string is invalid, with a descriptive message.

## Usage
