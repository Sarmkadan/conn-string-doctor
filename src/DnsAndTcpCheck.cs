using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that verifies DNS resolution and TCP reachability of the host.
/// </summary>
internal sealed class DnsAndTcpCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "Reachability";

    /// <inheritdoc />
    public async Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        var result = new DiagnosticResult(Name);

        // Skip check for SQLite provider
        if (info.Provider == DbProvider.Sqlite)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(info.Server))
        {
            result.AddError("Server/host is not specified.");
            return result;
        }

        // DNS resolution with timing
        var dnsStopwatch = Stopwatch.StartNew();
        IPAddress[]? addresses = null;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(info.Server).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.AddError($"DNS resolution failed: {ex.Message}");
            return result;
        }
        dnsStopwatch.Stop();

        if (dnsStopwatch.Elapsed > TimeSpan.FromSeconds(1))
        {
            result.AddWarning($"DNS resolution took {dnsStopwatch.Elapsed.TotalSeconds:F2}s.");
        }

        // Determine port
        int port = info.Port ?? ConnectionStringParser.DefaultPort(info.Provider);

        // TCP connection with timeout
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(info.Server, port);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), ct);

        var completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

        if (completedTask == timeoutTask)
        {
            result.AddError("Connection timed out after 5 seconds. Проверь firewall/VPN/port.");
        }
        else
        {
            try
            {
                await connectTask.ConfigureAwait(false);
                // Connection succeeded – nothing to report
            }
            catch (Exception ex)
            {
                result.AddError($"TCP connection failed: {ex.Message}. Проверь firewall/VPN/port.");
            }
        }

        return result;
    }
}
