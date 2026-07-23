using System.Data.Common;
using System.Text;

namespace ConnStringDoctor;

/// <summary>
/// Provides comparison functionality for connection strings.
/// </summary>
public static class ConnectionStringComparator
{
	/// <summary>
	/// Compares two connection strings semantically by normalizing both inputs first.
	/// </summary>
	/// <param name="connectionStringA">First connection string to compare</param>
	/// <param name="connectionStringB">Second connection string to compare</param>
	/// <param name="redact">Whether to redact sensitive information</param>
	/// <param name="showAll">Whether to show all keys including matching ones</param>
	/// <returns>A detailed comparison report</returns>
	/// <exception cref="ArgumentNullException">Thrown if either connection string is null or empty</exception>
	public static string Compare(string connectionStringA, string connectionStringB, bool redact = true, bool showAll = false)
	{
		ArgumentException.ThrowIfNullOrEmpty(connectionStringA, nameof(connectionStringA));
		ArgumentException.ThrowIfNullOrEmpty(connectionStringB, nameof(connectionStringB));

		// Normalize both connection strings to canonical form
		var normalizedA = ConnectionStringNormalizer.Normalize(connectionStringA, redact);
		var normalizedB = ConnectionStringNormalizer.Normalize(connectionStringB, redact);

		// Parse both connection strings for metadata
		var infoA = ConnectionStringParser.Parse(connectionStringA);
		var infoB = ConnectionStringParser.Parse(connectionStringB);

		// Parse normalized strings to dictionaries for comparison
		var dictA = ParseToDictionary(normalizedA);
		var dictB = ParseToDictionary(normalizedB);

		var builder = new StringBuilder();
		builder.AppendLine("=== Connection String Comparison ===");
		builder.AppendLine();

		// Header with basic info
		builder.AppendLine("Connection String A:");
		builder.AppendLine($" Original: {Truncate(connectionStringA, 80)}");
		builder.AppendLine($" Normalized: {Truncate(normalizedA, 80)}");
		builder.AppendLine($" Provider: {infoA.Provider}");
		builder.AppendLine($" Server: {infoA.Server}{(infoA.Port.HasValue ? $":{infoA.Port}" : "")}");
		builder.AppendLine($" Database: {infoA.Database}");
		builder.AppendLine($" User: {infoA.User}");
		builder.AppendLine();

		builder.AppendLine("Connection String B:");
		builder.AppendLine($" Original: {Truncate(connectionStringB, 80)}");
		builder.AppendLine($" Normalized: {Truncate(normalizedB, 80)}");
		builder.AppendLine($" Provider: {infoB.Provider}");
		builder.AppendLine($" Server: {infoB.Server}{(infoB.Port.HasValue ? $":{infoB.Port}" : "")}");
		builder.AppendLine($" Database: {infoB.Database}");
		builder.AppendLine($" User: {infoB.User}");
		builder.AppendLine();

		// Compare keys
		var allKeys = dictA.Keys.Concat(dictB.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		allKeys.Sort(StringComparer.OrdinalIgnoreCase);

		builder.AppendLine("=== Semantic Comparison ===");
		builder.AppendLine();

		bool hasDifferences = false;

		foreach (var key in allKeys)
		{
			var valueA = dictA.TryGetValue(key, out var v) ? v : null;
			var valueB = dictB.TryGetValue(key, out var v2) ? v2 : null;

			bool inA = dictA.ContainsKey(key);
			bool inB = dictB.ContainsKey(key);

			if (!showAll && !inA && !inB)
			{
				continue;
			}

			if (!hasDifferences && (inA != inB || valueA != valueB))
			{
				hasDifferences = true;
			}

			if (inA && inB)
			{
				// Key exists in both
				if (valueA == valueB)
				{
					if (showAll)
					{
						builder.AppendLine($"✓ {key} = {FormatValue(valueA)}");
					}
				}
				else
				{
					builder.AppendLine($"✗ {key}");
					builder.AppendLine($" A: {FormatValue(valueA)}");
					builder.AppendLine($" B: {FormatValue(valueB)}");
				}
			}
			else if (inA)
			{
				// Only in A
				builder.AppendLine($"⊖ {key} (only in A)");
				builder.AppendLine($" A: {FormatValue(valueA)}");
				builder.AppendLine(" B: <not present>");
			}
			else
			{
				// Only in B
				builder.AppendLine($"⊕ {key} (only in B)");
				builder.AppendLine(" A: <not present>");
				builder.AppendLine($" B: {FormatValue(valueB)}");
			}
		}

		if (!hasDifferences && !showAll)
		{
			builder.AppendLine("No differences found (use --show-all to see all keys)");
		}

		return builder.ToString();
	}

	/// <summary>
	/// Compares two connection strings semantically and returns a structured result.
	/// </summary>
	/// <param name="connectionStringA">First connection string to compare</param>
	/// <param name="connectionStringB">Second connection string to compare</param>
	/// <param name="redact">Whether to redact sensitive information</param>
	/// <returns>A ComparisonResult containing three buckets of differences</returns>
	/// <exception cref="ArgumentNullException">Thrown if either connection string is null or empty</exception>
	public static ComparisonResult CompareSemantically(string connectionStringA, string connectionStringB, bool redact = true)
	{
		ArgumentException.ThrowIfNullOrEmpty(connectionStringA, nameof(connectionStringA));
		ArgumentException.ThrowIfNullOrEmpty(connectionStringB, nameof(connectionStringB));

		// Normalize both connection strings to canonical form
		var normalizedA = ConnectionStringNormalizer.Normalize(connectionStringA, redact);
		var normalizedB = ConnectionStringNormalizer.Normalize(connectionStringB, redact);

		// Parse normalized strings to dictionaries for comparison
		var dictA = ParseToDictionary(normalizedA);
		var dictB = ParseToDictionary(normalizedB);

		var result = new ComparisonResult();

		// Get all unique keys from both dictionaries
		var allKeys = dictA.Keys.Concat(dictB.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

		foreach (var key in allKeys)
		{
			var valueA = dictA.TryGetValue(key, out var v) ? v : null;
			var valueB = dictB.TryGetValue(key, out var v2) ? v2 : null;

			bool inA = dictA.ContainsKey(key);
			bool inB = dictB.ContainsKey(key);

			if (inA && inB)
			{
				// Key exists in both - check if values are equal
				if (valueA == valueB)
				{
					// Semantically equal - different spelling (canonical form is the same)
					result.SemanticallyEqualKeys.Add(key);
				}
				else
				{
					// Genuinely different values
					result.DifferentValues.Add((new KeyValuePair<string, string>(key, valueA ?? string.Empty),
						new KeyValuePair<string, string>(key, valueB ?? string.Empty)));
				}
			}
			else if (inA)
			{
				// Key only in A
				result.KeysOnlyInA.Add(key);
			}
			else
			{
				// Key only in B
				result.KeysOnlyInB.Add(key);
			}
		}

		return result;
	}

	/// <summary>
	/// Parses a connection string to a dictionary without redacting.
	/// </summary>
	/// <param name="connectionString">The connection string to parse</param>
	/// <returns>A dictionary of key-value pairs</returns>
	private static Dictionary<string, string> ParseToDictionary(string connectionString)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return result;
		}

		try
		{
			var builder = new DbConnectionStringBuilder
			{
				ConnectionString = connectionString
			};

			foreach (string key in builder.Keys)
			{
				result[key] = builder[key]?.ToString() ?? string.Empty;
			}
		}
		catch (ArgumentException)
		{
			// If parsing fails, return empty dictionary
		}

		return result;
	}

	/// <summary>
	/// Formats a value for display, truncating long values.
	/// </summary>
	/// <param name="value">The value to format</param>
	/// <returns>The formatted value</returns>
	private static string FormatValue(string? value)
	{
		if (value == null)
		{
			return "<null>";
		}

		if (value.Length > 100)
		{
			return $"{value.Substring(0, 97)}... ({value.Length} chars)";
		}

		return value;
	}

	/// <summary>
	/// Truncates a string if it's too long.
	/// </summary>
	/// <param name="value">The string to truncate</param>
	/// <param name="maxLength">The maximum length</param>
	/// <returns>The truncated string</returns>
	private static string Truncate(string value, int maxLength)
	{
		if (value.Length <= maxLength)
		{
			return value;
		}

		return value.Substring(0, maxLength - 3) + "...";
	}
}

/// <summary>
/// Represents the result of a semantic comparison between two connection strings.
/// </summary>
public class ComparisonResult
{
	/// <summary>
	/// Gets the list of keys that are semantically equal but may have different spelling.
	/// These keys have the same canonical form and same value after normalization.
	/// </summary>
	public List<string> SemanticallyEqualKeys { get; } = new List<string>();

	/// <summary>
	/// Gets the list of key-value pairs that have genuinely different values.
	/// </summary>
	public List<(KeyValuePair<string, string> KeyA, KeyValuePair<string, string> KeyB)> DifferentValues { get; } = new List<(KeyValuePair<string, string>, KeyValuePair<string, string>)>();

	/// <summary>
	/// Gets the list of keys that are only present in the first connection string.
	/// </summary>
	public List<string> KeysOnlyInA { get; } = new List<string>();

	/// <summary>
	/// Gets the list of keys that are only present in the second connection string.
	/// </summary>
	public List<string> KeysOnlyInB { get; } = new List<string>();

	/// <summary>
	/// Gets a value indicating whether the connection strings are semantically equivalent.
	/// </summary>
	public bool AreSemanticallyEquivalent => DifferentValues.Count == 0 && KeysOnlyInA.Count == 0 && KeysOnlyInB.Count == 0;

	/// <summary>
	/// Gets a value indicating whether there are any differences at all.
	/// </summary>
	public bool HasDifferences => !AreSemanticallyEquivalent;
}