using System;
using System.Diagnostics;
using System.Globalization;
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
    private readonly TimeSpan _connectTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsAndTcpCheck"/> class.
    /// </summary>
    /// <param name="connectTimeout">TCP connection timeout. If not specified, defaults to 5 seconds.</param>
    public DnsAndTcpCheck(TimeSpan? connectTimeout = null)
    {
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public string Name => "Reachability";

        /// <inheritdoc />
        public string Description => "Verifies DNS resolution and TCP reachability of the host";

    /// <inheritdoc />
    public async Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        // SQLite is file based; there is nothing to resolve or connect to.
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
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(info.Server, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.AddError($"DNS resolution failed: {ex.Message}");
            return result;
        }
        dnsStopwatch.Stop();

        if (addresses.Length == 0)
        {
            result.AddError($"DNS resolution returned no addresses for '{info.Server}'.");
            return result;
        }

        if (dnsStopwatch.Elapsed > TimeSpan.FromSeconds(1))
        {
            result.AddWarning(string.Create(CultureInfo.InvariantCulture, $"DNS resolution took {dnsStopwatch.Elapsed.TotalSeconds:F2}s."));
        }

        // Determine port
        int port = info.Port ?? ConnectionStringParser.DefaultPort(info.Provider);
        if (port <= 0)
        {
            result.AddWarning("No port specified and no default port is known for the provider; skipping TCP check.");
            return result;
        }

        // TCP connection with timeout
        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_connectTimeout);

        try
        {
            await client.ConnectAsync(info.Server, port, timeoutCts.Token).ConfigureAwait(false);
            // Connection succeeded - nothing to report
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            result.AddError($"Connection timed out after {_connectTimeout.TotalSeconds:F0} seconds. Check firewall, VPN, and port settings.");
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            result.AddError($"TCP connection failed: {ex.Message}. Check firewall, VPN, and port settings.");
        }

        return result;
    }
}
