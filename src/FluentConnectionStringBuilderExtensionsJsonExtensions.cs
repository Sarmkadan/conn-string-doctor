using System;
using System.Text.Json;

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides JSON serialization helpers for <see cref="FluentConnectionStringBuilder"/>.
    /// </summary>
    public static class FluentConnectionStringBuilderExtensionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the specified <see cref="FluentConnectionStringBuilder"/> to a JSON string.
        /// </summary>
        /// <param name="value">The builder instance to serialize.</param>
        /// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation.</param>
        /// <returns>A JSON representation of the builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this FluentConnectionStringBuilder value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);
            var options = indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options;
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into a new <see cref="FluentConnectionStringBuilder"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A new <see cref="FluentConnectionStringBuilder"/> populated from the JSON, or <c>null</c> if the JSON does not represent a valid object.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or an empty string.</exception>
        public static FluentConnectionStringBuilder? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<FluentConnectionStringBuilder>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="FluentConnectionStringBuilder"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">When this method returns, contains the deserialized <see cref="FluentConnectionStringBuilder"/> if the operation succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or an empty string.</exception>
        public static bool TryFromJson(string json, out FluentConnectionStringBuilder? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            try
            {
                value = JsonSerializer.Deserialize<FluentConnectionStringBuilder>(json, _options);
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
