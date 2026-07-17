using System;
using System.Text.Json;

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="ConnectionStringConverter"/>.
    /// </summary>
    public static class ConnectionStringConverterJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes a <see cref="ConnectionStringConverter"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The converter instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the converter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static string ToJson(this ConnectionStringConverter value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (indented)
            {
                _jsonSerializerOptions.WriteIndented = true;
            }
            else
            {
                _jsonSerializerOptions.WriteIndented = false;
            }

            return JsonSerializer.Serialize(value, _jsonSerializerOptions);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="ConnectionStringConverter"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized converter instance, or <see langword="null"/> if deserialization fails.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
        public static ConnectionStringConverter? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                return JsonSerializer.Deserialize<ConnectionStringConverter>(json, _jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="ConnectionStringConverter"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized converter instance if successful; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
        public static bool TryFromJson(string json, out ConnectionStringConverter? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<ConnectionStringConverter>(json, _jsonSerializerOptions);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
