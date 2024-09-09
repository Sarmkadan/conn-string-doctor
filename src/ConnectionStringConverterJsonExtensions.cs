using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConnStringDoctor
{
    public static class ConnectionStringConverterJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string ToJson(this ConnectionStringConverter value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (indented)
            {
                _jsonSerializerOptions.WriteIndented = true;
            }
            return JsonSerializer.Serialize(value, _jsonSerializerOptions);
        }

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
