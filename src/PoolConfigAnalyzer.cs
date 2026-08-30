#nullable enable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Public analyzer for connection string pooling configuration that provides rule-based diagnostics
/// with actionable warnings, suggested fixes, and unique rule IDs.
/// </summary>
public sealed class PoolConfigAnalyzer : IDiagnosticCheck
{
    private const int DefaultMinPoolSize = 0; // Providers default to no pre-created pooled connections.
    private const int DefaultMaxPoolSize = 100; // Common provider default used by the low-timeout diagnostic.
    private const int LowConnectTimeoutThresholdSeconds = 10; // Below this, pool exhaustion can resemble a timeout.

    private const string PoolingKeyword = "Pooling";
    private const string DisabledNumericValue = "0";
    private const string Pool001MessagePrefix = "[POOL-001]";

    private static readonly string[] MaxPoolSizeKeywords = ["Max Pool Size", "Maximum Pool Size"];
    private static readonly string[] MinPoolSizeKeywords = ["Min Pool Size", "Minimum Pool Size"];
    private static readonly string[] ConnectTimeoutKeywords = ["Connect Timeout", "Connection Timeout", "Timeout"];
    private static readonly string[] ConnectionLifetimeKeywords = ["Connection Lifetime"];
    private static readonly string[] PoolingJustificationKeywords = ["PoolingJustification", "Justification", "Reason"];

    /// <summary>
    /// Gets the name of the diagnostic check.
    /// </summary>
    public string Name => PoolingKeyword;

    /// <summary>
    /// Gets the description of the diagnostic check.
    /// </summary>
    public string Description => "Analyzes connection string pooling configuration for common misconfigurations";

    /// <summary>
    /// Runs the diagnostic analysis on the connection string.
    /// </summary>
    /// <param name="info">Parsed connection string information.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to a <see cref="DiagnosticResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is null.</exception>
    public Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        // Run all pooling rule checks
        RunPoolingDisabledCheck(info, result);
        RunMaxPoolSizeLessThanMinPoolSizeCheck(info, result);
        RunConnectionLifetimeVsConnectTimeoutCheck(info, result);
        RunDefaultMaxPoolSizeWithLowTimeoutCheck(info, result);
        RunPoolingFalseWithoutJustificationCheck(info, result);

        return Task.FromResult(result);
    }

    private void RunPoolingDisabledCheck(ConnectionStringInfo info, DiagnosticResult result)
    {
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

        if (TryGet(out var poolingValue, PoolingKeyword))
        {
            if (bool.TryParse(poolingValue, out var poolingBool) && !poolingBool)
            {
                result.AddWarning(
                    $"{Pool001MessagePrefix} Pooling is explicitly disabled, which prevents connection pooling benefits. " +
                    "Remove the Pooling=false setting or provide a justification in a comment if pooling is intentionally disabled for specific reasons."
                );
            }
            else if (poolingValue == DisabledNumericValue)
            {
                result.AddWarning(
                    $"{Pool001MessagePrefix} Pooling is explicitly disabled with value '0', which prevents connection pooling benefits. " +
                    "Remove the Pooling=0 setting or provide a justification in a comment if pooling is intentionally disabled for specific reasons."
                );
            }
        }
    }

    private void RunMaxPoolSizeLessThanMinPoolSizeCheck(ConnectionStringInfo info, DiagnosticResult result)
    {
        static bool TryParseInt(string? text, out int parsed) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

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

        int? maxPoolSize = null;
        int? minPoolSize = null;

        if (TryGet(out var maxPoolSizeStr, MaxPoolSizeKeywords) && TryParseInt(maxPoolSizeStr, out var maxParsed))
        {
            maxPoolSize = maxParsed;
        }

        if (TryGet(out var minPoolSizeStr, MinPoolSizeKeywords) && TryParseInt(minPoolSizeStr, out var minParsed))
        {
            minPoolSize = minParsed;
        }

        // Only check if both values are present and max is less than min
        if (minPoolSize.HasValue && maxPoolSize.HasValue && minPoolSize.Value > maxPoolSize.Value)
        {
            result.AddError(
                $"[POOL-002] Min Pool Size ({minPoolSize.Value}) cannot be greater than Max Pool Size ({maxPoolSize.Value}). " +
                $"Set Min Pool Size to a value less than or equal to Max Pool Size. For example: Min Pool Size={maxPoolSize.Value - 1} or remove Min Pool Size if not needed."
            );
        }
    }

    private void RunConnectionLifetimeVsConnectTimeoutCheck(ConnectionStringInfo info, DiagnosticResult result)
    {
        static bool TryParseInt(string? text, out int parsed) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

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

        int? connectTimeout = null;
        int? connectionLifetime = null;

        if (TryGet(out var timeoutStr, ConnectTimeoutKeywords) && TryParseInt(timeoutStr, out var timeout))
        {
            connectTimeout = timeout;
        }

        if (TryGet(out var lifetimeStr, ConnectionLifetimeKeywords) && TryParseInt(lifetimeStr, out var lifetime))
        {
            connectionLifetime = lifetime;
        }

        // Check if Connection Lifetime is shorter than Connect Timeout
        if (connectTimeout.HasValue && connectionLifetime.HasValue && connectionLifetime.Value < connectTimeout.Value)
        {
            result.AddWarning(
                $"[POOL-003] Connection Lifetime ({connectionLifetime.Value}s) is shorter than Connect Timeout ({connectTimeout.Value}s), " +
                "which may cause premature connection recycling. " +
                "Increase Connection Lifetime to be greater than Connect Timeout, or remove Connection Lifetime if the default behavior is acceptable. " +
                "A good starting point is Connection Lifetime=3600 (1 hour) for most applications."
            );
        }
    }

    private void RunDefaultMaxPoolSizeWithLowTimeoutCheck(ConnectionStringInfo info, DiagnosticResult result)
    {
        static bool TryParseInt(string? text, out int parsed) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

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

        int? maxPoolSize = null;
        int? connectTimeout = null;

        // Check if Max Pool Size is at its provider default.
        if (TryGet(out var maxPoolSizeStr, MaxPoolSizeKeywords) && TryParseInt(maxPoolSizeStr, out var maxParsed) && maxParsed == DefaultMaxPoolSize)
        {
            maxPoolSize = maxParsed;
        }
        else if (!TryGet(out _, MaxPoolSizeKeywords))
        {
            // Max Pool Size not specified, so use the provider default.
            maxPoolSize = DefaultMaxPoolSize;
        }

        // Check Connect Timeout
        if (TryGet(out var timeoutStr, ConnectTimeoutKeywords) && TryParseInt(timeoutStr, out var timeout))
        {
            connectTimeout = timeout;
        }

        // If Max Pool Size is 100 (default) and Connect Timeout is very low (< 10 seconds),
        // this can masquerade as a pool exhaustion timeout issue
        if (maxPoolSize == DefaultMaxPoolSize && connectTimeout.HasValue && connectTimeout.Value < LowConnectTimeoutThresholdSeconds)
        {
            result.AddWarning(
                $"[POOL-004] Max Pool Size is set to default (100) with very low Connect Timeout ({connectTimeout.Value}s), " +
                "which may cause pool exhaustion before timeouts occur. " +
                "Either increase Connect Timeout to at least 30 seconds, or increase Max Pool Size to a higher value like 200-500 to handle transient failures better. " +
                "Consider both options for production workloads."
            );
        }
    }

    private void RunPoolingFalseWithoutJustificationCheck(ConnectionStringInfo info, DiagnosticResult result)
    {
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

        if (TryGet(out var poolingValue, PoolingKeyword) && (bool.TryParse(poolingValue, out var poolingBool) && !poolingBool || poolingValue == DisabledNumericValue))
        {
            // Check if there's a justification key present
            bool hasJustification = TryGet(out _, PoolingJustificationKeywords);

            if (!hasJustification)
            {
                result.AddWarning(
                    $"{Pool001MessagePrefix} Pooling is disabled without providing a justification. " +
                    "Either remove Pooling=false or add a PoolingJustification key explaining why pooling is disabled. " +
                    "Example: PoolingJustification='Connection pooling disabled due to XA transaction manager limitations'"
                );
            }
        }
    }
}
