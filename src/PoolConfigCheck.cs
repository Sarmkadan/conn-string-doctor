#nullable enable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that validates pooling related settings in a connection string.
/// </summary>
internal sealed class PoolConfigCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "Pooling";

    /// <inheritdoc />
    public string Description => "Validates pooling related settings in connection strings";

    /// <inheritdoc />
    public Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        // Helper to get a property value under any of the provider-specific synonyms (case-insensitive)
        bool TryGet(out string? value, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (info.Properties.TryGetValue(key, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        static bool TryParseInt(string? text, out int parsed) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

        // 1. Pooling = false -> warning
        if (TryGet(out var poolingValue, "Pooling"))
        {
            if (bool.TryParse(poolingValue, out var poolingBool))
            {
                if (!poolingBool)
                {
                    result.AddWarning("Pooling is disabled.");
                }
            }
            else if (poolingValue == "0")
            {
                result.AddWarning("Pooling is disabled (value '0').");
            }
        }

        // 2. MaxPoolSize handling
        int? maxPoolSize = null;
        if (TryGet(out var maxPoolSizeStr, "Max Pool Size", "Maximum Pool Size") && TryParseInt(maxPoolSizeStr, out var maxParsed))
        {
            maxPoolSize = maxParsed;

            // 2a. MaxPoolSize > 500 -> warning
            if (maxParsed > 500)
            {
                result.AddWarning($"Max Pool Size is {maxParsed}, which exceeds the recommended maximum of 500.");
            }
        }
        else
        {
            // MaxPoolSize not specified -> info about default (treated as warning level)
            result.AddWarning("Max Pool Size not specified; default is 100.");
        }

        // 3. MinPoolSize handling
        int? minPoolSize = null;
        if (TryGet(out var minPoolSizeStr, "Min Pool Size", "Minimum Pool Size") && TryParseInt(minPoolSizeStr, out var minParsed))
        {
            minPoolSize = minParsed;
        }

        // 4. MinPoolSize > MaxPoolSize -> error (fail)
        if (minPoolSize.HasValue && maxPoolSize.HasValue && minPoolSize.Value > maxPoolSize.Value)
        {
            result.AddError($"Min Pool Size ({minPoolSize.Value}) is greater than Max Pool Size ({maxPoolSize.Value}).");
        }

        // 5. Connect Timeout handling
        if (TryGet(out var timeoutStr, "Connect Timeout", "Connection Timeout", "Timeout") && TryParseInt(timeoutStr, out var timeout))
        {
            if (timeout > 30)
            {
                result.AddWarning($"Connect Timeout is {timeout} seconds, which may mask network problems.");
            }
        }
        else
        {
            // No Connect Timeout specified -> recommendation
            result.AddWarning("Connect Timeout not specified; consider setting it explicitly.");
        }

        return Task.FromResult(result);
    }
}
