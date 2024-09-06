using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ConnStringDoctor;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="FluentConnectionStringBuilder"/>.
/// </summary>
public static class FluentConnectionStringBuilderJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = false,
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(_jsonOptions)
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Serializes the configuration of a <see cref="FluentConnectionStringBuilder"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The connection string builder to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON representation of the connection string builder configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this FluentConnectionStringBuilder value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value.CaptureState(), indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string produced by <see cref="ToJson"/> back into a
    /// <see cref="FluentConnectionStringBuilder"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// A <see cref="FluentConnectionStringBuilder"/> instance if the JSON is valid and contains a provider;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    public static FluentConnectionStringBuilder? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var state = JsonSerializer.Deserialize<FluentConnectionStringBuilderState>(json, _jsonOptions);
            return state is null || string.IsNullOrWhiteSpace(state.Provider)
                ? null
                : FluentConnectionStringBuilder.FromState(state);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string produced by <see cref="ToJson"/> into a
    /// <see cref="FluentConnectionStringBuilder"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized builder if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJson(string json, out FluentConnectionStringBuilder? value)
    {
        value = FromJson(json);
        return value is not null;
    }
}
