# ConnectionStringInfoExtensions

Extension methods for analyzing and manipulating database connection strings, providing common operations such as endpoint extraction, local database detection, sanitization, and authentication requirement checks.

## API

### `GetServerEndpoint`

Extracts the server endpoint from a connection string.

- **Parameters**
  - `connectionString` (`string`): The connection string to parse.

- **Return Value**
  - `string`: The server endpoint (e.g., `localhost`, `127.0.0.1`, `server.example.com`).

- **Exceptions**
  - Throws `ArgumentNullException` if `connectionString` is `null`.
  - Throws `FormatException` if the connection string is malformed and the server endpoint cannot be extracted.

---

### `IsLocalDatabase`

Determines whether the connection string points to a local database instance.

- **Parameters**
  - `connectionString` (`string`): The connection string to evaluate.

- **Return Value**
  - `bool`: `true` if the server endpoint is a local address (`localhost`, `127.0.0.1`, `::1`, or `.` for SQL Server); otherwise, `false`.

- **Exceptions**
  - Throws `ArgumentNullException` if `connectionString` is `null`.

---

### `ToSanitizedString`

Removes sensitive information (e.g., passwords) from a connection string.

- **Parameters**
  - `connectionString` (`string`): The connection string to sanitize.

- **Return Value**
  - `string`: A sanitized version of the connection string with sensitive data replaced by placeholders (e.g., `Password=*****`).

- **Exceptions**
  - Throws `ArgumentNullException` if `connectionString` is `null`.

---

### `RequiresAuthentication`

Checks whether the connection string specifies authentication credentials.

- **Parameters**
  - `connectionString` (`string`): The connection string to inspect.

- **Return Value**
  - `bool`: `true` if the connection string includes user credentials (e.g., `User ID=`, `UID=`, `Password=`); otherwise, `false`.

- **Exceptions**
  - Throws `ArgumentNullException` if `connectionString` is `null`.

## Usage
