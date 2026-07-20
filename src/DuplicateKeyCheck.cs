#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that detects duplicate keys (case-insensitive) in connection string properties.
/// Reports which value wins when the same key appears multiple times.
/// </summary>
internal sealed class DuplicateKeyCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "DuplicateKeys";

    /// <inheritdoc />
    public Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        // Check for duplicate keys (case-insensitive)
        var duplicateKeys = FindDuplicateKeys(info.Properties);

        if (duplicateKeys.Count > 0)
        {
            var message = new System.Text.StringBuilder();
            message.Append($"Found {duplicateKeys.Count} duplicate key(s): ");

            foreach (var (key, values) in duplicateKeys)
            {
                message.Append($"'{key}'");
                message.Append($" with values: {string.Join(", ", values.Select(v => $"'{v}'"))} ");
                message.Append("(last value wins)");
                message.Append("; ");
            }

            result.SetMessage(message.ToString());
            result.SetDetails("Connection string contains duplicate keys. Only the last occurrence is used by most parsers.");

            // Add warning for each duplicate key
            foreach (var (key, values) in duplicateKeys)
            {
                result.AddWarning($"Duplicate key '{key}' found {values.Count + 1} times in connection string");
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Finds duplicate keys in a case-insensitive dictionary.
    /// Returns a dictionary mapping each duplicate key to all its values (in order of appearance).
    /// </summary>
    private static Dictionary<string, List<string>> FindDuplicateKeys(IReadOnlyDictionary<string, string> properties)
    {
        var duplicates = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in properties)
        {
            if (seenKeys.Contains(kvp.Key))
            {
                // This key was seen before - it's a duplicate
                if (!duplicates.TryGetValue(kvp.Key, out var values))
                {
                    values = new List<string>();
                    duplicates[kvp.Key] = values;
                }
                values.Add(kvp.Value);
            }
            else
            {
                seenKeys.Add(kvp.Key);
            }
        }

        return duplicates;
    }
}
