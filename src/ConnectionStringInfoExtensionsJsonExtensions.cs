namespace ConnStringDoctor;

/// <summary>
/// Provides System.Text.Json serialization extension methods for <see cref="ConnectionStringInfo"/>.
/// </summary>
public static class ConnectionStringInfoExtensionsJsonExtensions
{
	private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new(System.Text.Json.JsonSerializerOptions.Default)
	{
		PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <summary>
	/// Serializes the <see cref="ConnectionStringInfo"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The connection string information to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the connection string information.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this ConnectionStringInfo value, bool indented = false)
		=> System.Text.Json.JsonSerializer.Serialize(value, indented ? new System.Text.Json.JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);

	/// <summary>
	/// Deserializes a JSON string to a <see cref="ConnectionStringInfo"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>A deserialized <see cref="ConnectionStringInfo"/> instance, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
	/// <exception cref="System.Text.Json.JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static ConnectionStringInfo? FromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return System.Text.Json.JsonSerializer.Deserialize<ConnectionStringInfo>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="ConnectionStringInfo"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized instance if successful; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
	public static bool TryFromJson(string json, out ConnectionStringInfo? value)
	{
		value = null;

		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}

		try
		{
			value = System.Text.Json.JsonSerializer.Deserialize<ConnectionStringInfo>(json, _jsonOptions);
			return true;
		}
		catch (System.Text.Json.JsonException)
		{
			return false;
		}
	}
}