using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides JSON serialization helpers for <see cref="FluentConnectionStringBuilder"/>.
    /// </summary>
    public static class FluentConnectionStringBuilderJsonExtensionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Converts the specified <see cref="FluentConnectionStringBuilder"/> to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="FluentConnectionStringBuilder"/> to convert.</param>
        /// <param name="indented">Whether to format the JSON string with indentation.</param>
        /// <returns>A JSON string representation of the <see cref="FluentConnectionStringBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static string ToJson(this FluentConnectionStringBuilder value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);
            return JsonSerializer.Serialize(value, _jsonSerializerOptions);
        }

        /// <summary>
        /// Attempts to convert the specified JSON string to a <see cref="FluentConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="json">The JSON string to convert.</param>
        /// <returns>A <see cref="FluentConnectionStringBuilder"/> instance if the conversion is successful; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown if the JSON string is invalid.</exception>
        public static FluentConnectionStringBuilder? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            try
            {
                return JsonSerializer.Deserialize<FluentConnectionStringBuilder>(json, _jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to convert the specified JSON string to a <see cref="FluentConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="json">The JSON string to convert.</param>
        /// <param name="value">The converted <see cref="FluentConnectionStringBuilder"/> instance if the conversion is successful; otherwise, null.</param>
        /// <returns>True if the conversion is successful; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out FluentConnectionStringBuilder? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            try
            {
                value = JsonSerializer.Deserialize<FluentConnectionStringBuilder>(json, _jsonSerializerOptions);
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
