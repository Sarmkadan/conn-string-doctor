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
    /// <summary>
    /// Gets the name of the diagnostic check.
    /// </summary>
    public string Name => "Timeout";

    /// <summary>
    /// Gets the description of the diagnostic check.
    /// </summary>
    public string Description => "Validates timeout related settings in connection strings";

    /// <summary>
    /// Runs the diagnostic check asynchronously on the provided <see cref="ConnectionStringInfo"/>.
    /// </summary>
    /// <param name="info">The parsed connection string information to evaluate.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{DiagnosticResult}"/> that resolves to a <see cref="DiagnosticResult"/>
    /// containing any warnings or errors identified during the check.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <c>null</c>.</exception>
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
        if (TryGet(out var timeoutStr, "Connect Timeout", "Connection Timeout", "Timeout") && TryParseInt(timeoutStr, out var timeout))
        {
            if (timeout > 60)
            {
                result.AddWarning($"Connect Timeout is {timeout} seconds, which may hide network issues.");
            }
        }
        else
        {
            // No Connect Timeout specified -> recommendation (info level)
            result.AddWarning("Connect Timeout not specified; default is 15 seconds.");
        }

        // 2. Command Timeout handling
        if (TryGet(out var cmdTimeoutStr, "Command Timeout", "CommandTimeout") && TryParseInt(cmdTimeoutStr, out var cmdTimeout))
        {
            if (cmdTimeout == 0)
            {
                result.AddWarning("Command Timeout is set to 0, which means infinite timeout.");
            }
        }

        return Task.FromResult(result);
    }
}
