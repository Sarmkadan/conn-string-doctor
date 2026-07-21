using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for <see cref="ConnectionStringRedactor"/>.
    /// </summary>
    public static class ConnectionStringRedactorJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions _jsonOptionsIndented = new(_jsonOptions)
        {
            WriteIndented = true
        };

        /// <summary>
        /// Redacts secrets in a connection string and returns the redacted result as JSON.
        /// </summary>
        /// <param name="connectionString">The original connection string to redact.</param>
        /// <param name="mask">The mask to replace secret values with. Defaults to "***".</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON object containing the redacted connection string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is null.</exception>
        public static string RedactToJson(this string connectionString, string mask = "***", bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var redacted = ConnectionStringRedactor.Redact(connectionString, RedactionMode.Full, mask);
            var result = new { Original = connectionString, Redacted = redacted };

            return JsonSerializer.Serialize(result, indented ? _jsonOptionsIndented : _jsonOptions);
        }

        /// <summary>
        /// Redacts only the password in a connection string and returns the redacted result as JSON.
        /// </summary>
        /// <param name="connectionString">The original connection string to redact.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON object containing the original and password-redacted connection strings.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is null.</exception>
        public static string RedactKeepUserToJson(this string connectionString, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var redacted = ConnectionStringRedactor.RedactKeepUser(connectionString);
            var result = new { Original = connectionString, Redacted = redacted };

            return JsonSerializer.Serialize(result, indented ? _jsonOptionsIndented : _jsonOptions);
        }

        /// <summary>
        /// Checks if a connection string contains secrets and returns the result as JSON.
        /// </summary>
        /// <param name="connectionString">The connection string to check for secrets.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON object containing the original connection string and a boolean indicating if secrets were found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is null.</exception>
        public static string ContainsSecretsToJson(this string connectionString, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var hasSecrets = ConnectionStringRedactor.ContainsSecrets(connectionString);
            var result = new { ConnectionString = connectionString, HasSecrets = hasSecrets };

            return JsonSerializer.Serialize(result, indented ? _jsonOptionsIndented : _jsonOptions);
        }
    }
}
