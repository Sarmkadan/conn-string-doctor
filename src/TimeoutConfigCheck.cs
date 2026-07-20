#nullable enable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that validates timeout related settings in a connection string.
/// </summary>
internal sealed class TimeoutConfigCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "Timeouts";

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

        // 1. Connect Timeout handling
        if (TryGet(out var connectTimeoutStr, "Connect Timeout", "Connection Timeout", "Timeout"))
        {
            if (TryParseInt(connectTimeoutStr, out var connectTimeout) && connectTimeout > 0)
            {
                // Connect Timeout > 60s -> warning: hides network issues
                if (connectTimeout > 60)
                {
                    result.AddWarning($"Connect Timeout is {connectTimeout} seconds, which may mask network problems.");
                }
            }
        }
        else
        {
            // No Connect Timeout specified -> info about default assumption
            result.SetMessage("Connect Timeout not specified; default is typically 15 seconds.");
        }

        // 2. Command Timeout handling
        if (TryGet(out var commandTimeoutStr, "Command Timeout"))
        {
            if (TryParseInt(commandTimeoutStr, out var commandTimeout))
            {
                // Command Timeout of 0 -> warning: infinite
                if (commandTimeout == 0)
                {
                    result.AddWarning("Command Timeout is 0, which means infinite timeout.");
                }
            }
        }

        return Task.FromResult(result);
    }
}