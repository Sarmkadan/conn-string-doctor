#nullable enable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that validates timeout values in a connection string.
/// Flags Connect Timeout/Command Timeout values that are 0 (infinite), negative, or over 300s.
/// </summary>
internal sealed class TimeoutSanityCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "TimeoutSanity";

    /// <inheritdoc />
    public string Description => "Validates timeout values are within acceptable ranges (positive, non-zero, reasonable limits)";

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

        // 1. Connect Timeout validation
        if (TryGet(out var connectTimeoutStr, "Connect Timeout", "Connection Timeout", "Timeout") && TryParseInt(connectTimeoutStr, out var connectTimeout))
        {
            if (connectTimeout < 0)
            {
                result.AddError(
                $"Connect Timeout is {connectTimeout} seconds, which is negative and will cause immediate failure. Use a positive value.");
            }
            else if (connectTimeout == 0)
            {
                result.AddError(
                "Connect Timeout is set to 0, which means infinite timeout. This can cause application hangs. Use a positive value.");
            }
            else if (connectTimeout > 300)
            {
                result.AddWarning(
                $"Connect Timeout is {connectTimeout} seconds, which is greater than 300 seconds. Consider using a lower value to avoid hiding network issues.");
            }
        }

        // 2. Command Timeout validation
        if (TryGet(out var cmdTimeoutStr, "Command Timeout", "CommandTimeout") && TryParseInt(cmdTimeoutStr, out var cmdTimeout))
        {
            if (cmdTimeout < 0)
            {
                result.AddError(
                $"Command Timeout is {cmdTimeout} seconds, which is negative and will cause immediate failure. Use a positive value.");
            }
            else if (cmdTimeout == 0)
            {
                result.AddError(
                "Command Timeout is set to 0, which means infinite timeout. This can cause application hangs. Use a positive value.");
            }
            else if (cmdTimeout > 300)
            {
                result.AddWarning(
                $"Command Timeout is {cmdTimeout} seconds, which is greater than 300 seconds. Consider using a lower value for better resource management.");
            }
        }

        return Task.FromResult(result);
    }
}
