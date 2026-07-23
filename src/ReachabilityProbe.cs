using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ConnStringDoctor;

/// <summary>
/// Provides functionality to probe the reachability of a database server with timeout, retry, and cancellation support.
/// </summary>
public sealed class ReachabilityProbe : IDisposable
{
    private readonly ConnectionStringInfo _connectionStringInfo;
    private readonly TimeSpan _connectTimeout;
    private readonly int _maxAttempts;
    private readonly Random _random = Random.Shared;
    private bool _disposed;

    /// <summary>
    /// Gets the connection string information being probed.
    /// </summary>
    public ConnectionStringInfo ConnectionStringInfo => _connectionStringInfo;

    /// <summary>
    /// Gets the TCP connection timeout for each attempt.
    /// </summary>
    public TimeSpan ConnectTimeout => _connectTimeout;

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts => _maxAttempts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReachabilityProbe"/> class.
    /// </summary>
    /// <param name="connectionStringInfo">The connection string information to probe.</param>
    /// <param name="connectTimeout">The TCP connection timeout for each attempt. If not specified, defaults to 5 seconds.</param>
    /// <param name="maxAttempts">The maximum number of retry attempts. If not specified, defaults to 3.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionStringInfo"/> is null.</exception>
    public ReachabilityProbe(
        ConnectionStringInfo connectionStringInfo,
        TimeSpan? connectTimeout = null,
        int maxAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(connectionStringInfo);

        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");
        }

        _connectionStringInfo = connectionStringInfo;
        _connectTimeout = connectTimeout ?? GetDefaultConnectTimeout(connectionStringInfo);
        _maxAttempts = maxAttempts;
    }

    /// <summary>
    /// Probes the reachability of the server with the specified cancellation token.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the probe operation and contains the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ct"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the probe has been disposed.</exception>
    public async Task<ReachabilityProbeResult> ProbeAsync(CancellationToken ct = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReachabilityProbe));
        }

        var stopwatch = Stopwatch.StartNew();
        var attemptTimes = new List<TimeSpan>(_maxAttempts);
        ProbeStatus status = ProbeStatus.OtherFailure;
        Exception? exception = null;
        string? details = null;

        try
        {
            status = await ProbeCoreAsync(ct, attemptTimes).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            status = ProbeStatus.Timeout;
            exception = ex;
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            // Map specific socket errors to appropriate status
            if (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.ConnectionRefused)
            {
                status = ProbeStatus.ConnectionRefused;
            }
            else if (ex is SocketException socketEx2 &&
                     (socketEx2.SocketErrorCode == SocketError.HostNotFound ||
                      socketEx2.SocketErrorCode == SocketError.TryAgain))
            {
                status = ProbeStatus.DnsFailure;
            }
            else
            {
                status = ProbeStatus.OtherFailure;
            }
            exception = ex;
        }
        catch (Exception ex)
        {
            status = ProbeStatus.OtherFailure;
            exception = ex;
        }
        finally
        {
            stopwatch.Stop();
        }

        return new ReachabilityProbeResult(
            status,
            stopwatch.Elapsed,
            attemptTimes,
            exception,
            details);
    }

    private async Task<ProbeStatus> ProbeCoreAsync(
        CancellationToken ct,
        List<TimeSpan> attemptTimes)
    {
        // SQLite is file based; there is nothing to resolve or connect to.
        if (_connectionStringInfo.Provider == DbProvider.Sqlite)
        {
            return ProbeStatus.Reachable;
        }

        if (string.IsNullOrWhiteSpace(_connectionStringInfo.Server))
        {
            return ProbeStatus.DnsFailure;
        }

        int port = _connectionStringInfo.Port ?? ConnectionStringParser.DefaultPort(_connectionStringInfo.Provider);
        if (port <= 0)
        {
            return ProbeStatus.OtherFailure;
        }

        // Perform up to MaxAttempts probes with exponential backoff and jitter
        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            var attemptStopwatch = Stopwatch.StartNew();
            ProbeStatus attemptStatus = await ProbeSingleAttemptAsync(attempt, ct).ConfigureAwait(false);
            attemptStopwatch.Stop();
            attemptTimes.Add(attemptStopwatch.Elapsed);

            if (attemptStatus == ProbeStatus.Reachable)
            {
                return ProbeStatus.Reachable;
            }

            // Don't wait after the last attempt
            if (attempt < _maxAttempts)
            {
                await ApplyBackoffAsync(attempt).ConfigureAwait(false);
            }
        }

        return ProbeStatus.Timeout; // If we exhausted all attempts without success
    }

    private async Task<ProbeStatus> ProbeSingleAttemptAsync(int attemptNumber, CancellationToken ct)
    {
        try
        {
            // DNS resolution
            var dnsStopwatch = Stopwatch.StartNew();
            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(_connectionStringInfo.Server ?? string.Empty, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return ProbeStatus.DnsFailure;
            }
            dnsStopwatch.Stop();

            if (addresses.Length == 0)
            {
                return ProbeStatus.DnsFailure;
            }

            // TCP connection with timeout
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_connectTimeout);

            try
            {
                await client.ConnectAsync(_connectionStringInfo.Server, _connectionStringInfo.Port ?? ConnectionStringParser.DefaultPort(_connectionStringInfo.Provider), timeoutCts.Token).ConfigureAwait(false);
                return ProbeStatus.Reachable;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return ProbeStatus.Timeout;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return ProbeStatus.ConnectionRefused;
            }
            catch (SocketException)
            {
                return ProbeStatus.Timeout;
            }
            catch (Exception)
            {
                return ProbeStatus.OtherFailure;
            }
        }
        catch (OperationCanceledException)
        {
            return ProbeStatus.Timeout;
        }
    }

    private async Task ApplyBackoffAsync(int attemptNumber)
    {
        // Exponential backoff with jitter
        // Base delay: 1s * 2^(attempt-1)
        // Add jitter: random value between 0 and base delay
        // Cap at reasonable maximum
        int baseDelayMs = Math.Min(1000 * (int)Math.Pow(2, attemptNumber - 1), 10000);
        int jitterMs = _random.Next(0, baseDelayMs);
        int totalDelayMs = baseDelayMs + jitterMs;

        await Task.Delay(totalDelayMs).ConfigureAwait(false);
    }

    private static TimeSpan GetDefaultConnectTimeout(ConnectionStringInfo info)
    {
        // Try to get timeout from connection string properties
        if (info.Properties.TryGetValue("Connect Timeout", out var timeoutValue) &&
            int.TryParse(timeoutValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutSeconds) &&
            timeoutSeconds > 0)
        {
            return TimeSpan.FromSeconds(timeoutSeconds);
        }

        if (info.Properties.TryGetValue("Connection Timeout", out var connectionTimeoutValue) &&
            int.TryParse(connectionTimeoutValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var connectionTimeoutSeconds) &&
            connectionTimeoutSeconds > 0)
        {
            return TimeSpan.FromSeconds(connectionTimeoutSeconds);
        }

        // Default to 5 seconds
        return TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // No unmanaged resources to clean up in this class
            _disposed = true;
        }
    }
}