using System;
using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that inspects TLS handshake and certificate validation for database connections.
/// </summary>
internal sealed class TlsInspector : IDiagnosticCheck
{
    private readonly TimeSpan _connectTimeout;
    private readonly TimeSpan _handshakeTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlsInspector"/> class.
    /// </summary>
    /// <param name="connectTimeout">TCP connection timeout. If not specified, defaults to 5 seconds.</param>
    /// <param name="handshakeTimeout">TLS handshake timeout. If not specified, defaults to 10 seconds.</param>
    public TlsInspector(TimeSpan? connectTimeout = null, TimeSpan? handshakeTimeout = null)
    {
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(5);
        _handshakeTimeout = handshakeTimeout ?? TimeSpan.FromSeconds(10);
    }

    /// <inheritdoc />
    public string Name => "TlsHandshake";

    /// <inheritdoc />
    public string Description => "Inspects TLS handshake, certificate validity, and configuration compatibility";

    /// <inheritdoc />
    public async Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        // SQLite is file based; there is nothing to connect to via TLS
        if (info.Provider == DbProvider.Sqlite)
        {
            result.SetMessage("SQLite is file-based; TLS inspection not applicable.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(info.Server))
        {
            result.AddError("Server/host is not specified.");
            return result;
        }

        // Determine port - default to standard TLS ports based on provider
        int port = info.Port ?? GetDefaultTlsPort(info.Provider);
        if (port <= 0)
        {
            result.AddError("No port specified and no default TLS port is known for the provider.");
            return result;
        }

        // Check if encryption is explicitly disabled
        bool encryptEnabled = IsEncryptionEnabled(info);
        if (!encryptEnabled)
        {
            result.AddWarning("Encryption is disabled (Encrypt=false or not specified). TLS inspection skipped.");
            return result;
        }

        // Perform TLS handshake and certificate validation
        try
        {
            var tlsResult = await InspectTlsAsync(info.Server, port, info, ct).ConfigureAwait(false);

            result.SetMessage(tlsResult.Message);
            result.SetDetails(tlsResult.Details);

            // Add any warnings from the TLS inspection
            foreach (var warning in tlsResult.Warnings)
            {
                result.AddWarning(warning);
            }

            // Add any errors from the TLS inspection
            foreach (var error in tlsResult.Errors)
            {
                result.AddError(error);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.AddError($"TLS inspection failed: {ex.Message}");
        }

        return result;
    }

    private async Task<TlsInspectionResult> InspectTlsAsync(
        string host,
        int port,
        ConnectionStringInfo info,
        CancellationToken ct)
    {
        var result = new TlsInspectionResult();

        using var tcpClient = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_connectTimeout);

        // Connect to server
        await tcpClient.ConnectAsync(host, port, timeoutCts.Token).ConfigureAwait(false);

        using var sslStream = new SslStream(
            tcpClient.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: ValidateServerCertificate);

        // Perform TLS handshake
        var sslOptions = new SslClientAuthenticationOptions
        {
            TargetHost = host,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck
        };

        using var handshakeTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        handshakeTimeoutCts.CancelAfter(_handshakeTimeout);

        try
        {
            await sslStream.AuthenticateAsClientAsync(
                sslOptions,
                handshakeTimeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            result.Errors.Add("TLS handshake timed out.");
            return result;
        }
        catch (AuthenticationException ex)
        {
            result.Errors.Add($"TLS authentication failed: {ex.Message}");
            return result;
        }

        // Extract TLS information
        result.Message = $"TLS handshake successful with {sslStream.SslProtocol}";

        // Check cipher suite using NegotiatedCipherSuite (modern API)
        try
        {
            var negotiatedCipherSuite = sslStream.NegotiatedCipherSuite;
            result.Details = $"Negotiated Cipher Suite: {negotiatedCipherSuite}\n";

            // Security level assessment - basic check
            if (negotiatedCipherSuite.ToString().IndexOf("NULL", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Warnings.Add("Using NULL cipher suite (no encryption). This is a critical security issue.");
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Could not retrieve cipher suite: {ex.Message}");
        }

        // Validate certificate chain - we need to get the chain from the SSL stream
        try
        {
            var remoteCertificate = sslStream.RemoteCertificate;
            if (remoteCertificate != null)
            {
                var cert = new X509Certificate2(remoteCertificate);
                // Build certificate chain for validation
                X509Chain? certChain = null;
                try
                {
                    certChain = new X509Chain();
                    certChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    certChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    bool chainValid = certChain.Build(cert);

                    ValidateCertificate(cert, host, info, certChain, result);
                }
                finally
                {
                    certChain?.Dispose();
                }
            }
            else
            {
                result.Warnings.Add("No server certificate provided.");
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Certificate validation error: {ex.Message}");
        }

        // Check for SSL policy errors from validation callback
        if (_sslPolicyErrors.HasValue && _sslPolicyErrors.Value != SslPolicyErrors.None)
        {
            result.Warnings.Add($"Certificate validation issues detected: {_sslPolicyErrors}");
        }

        return result;
    }

    private bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        // This callback is called during the TLS handshake
        // We'll validate in the InspectTlsAsync method after handshake completes
        // Store any policy errors for later inspection
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
            _sslPolicyErrors = sslPolicyErrors;
        }
        return true; // Allow handshake to proceed, we'll validate after
    }

    private SslPolicyErrors? _sslPolicyErrors;

    private void ValidateCertificate(
        X509Certificate2 cert,
        string host,
        ConnectionStringInfo info,
        X509Chain? chain,
        TlsInspectionResult result)
    {
        // Check certificate validity period
        DateTime now = DateTime.UtcNow;
        DateTime notBefore = cert.NotBefore;
        DateTime notAfter = cert.NotAfter;

        result.Details += $"Certificate Valid From: {notBefore:yyyy-MM-dd}\n";
        result.Details += $"Certificate Valid Until: {notAfter:yyyy-MM-dd}\n";
        result.Details += $"Certificate Issuer: {cert.Issuer}\n";
        result.Details += $"Certificate Subject: {cert.Subject}\n";

        // Check if certificate is expired
        if (now < notBefore)
        {
            result.Errors.Add("Certificate is not yet valid (before NotBefore date).");
        }
        else if (now > notAfter)
        {
            result.Errors.Add("Certificate has expired.");
        }
        else
        {
            // Calculate days remaining
            TimeSpan validityPeriod = notAfter - now;
            if (validityPeriod.TotalDays < 30)
            {
                result.Warnings.Add($"Certificate expires in {validityPeriod.TotalDays:F0} days. Consider renewing soon.");
            }
        }

        // Check hostname matching
        string certHost = cert.GetNameInfo(X509NameType.DnsName, false);
        if (string.IsNullOrEmpty(certHost) || !certHost.Equals(host, StringComparison.OrdinalIgnoreCase))
        {
            // Try alternative methods to get hostname
            string subjectAltName = cert.GetNameInfo(X509NameType.UpnName, false) ??
                                  cert.GetNameInfo(X509NameType.EmailName, false);

            if (string.IsNullOrEmpty(subjectAltName) || !subjectAltName.Equals(host, StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add("Certificate hostname does not match server hostname. This may cause connection issues.");
            }
        }

        // Check certificate chain
        if (cert.Subject != cert.Issuer)
        {
            result.Details += "Certificate is not self-signed.\n";

            // Validate chain status
            if (chain != null)
            {
                try
                {
                    var chainStatus = chain.Build(new X509Certificate2(cert));
                    if (!chainStatus)
                    {
                        foreach (var status in chain.ChainStatus)
                        {
                            result.Warnings.Add($"Certificate chain issue: {status.StatusInformation}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Could not validate certificate chain: {ex.Message}");
                }
            }
        }
        else
        {
            result.Details += "Certificate is self-signed.\n";
        }
    }

    // Placeholder for future weak cipher detection
    // Kept for extensibility but simplified to avoid API complexity

    private bool IsEncryptionEnabled(ConnectionStringInfo info)
    {
        // Check Encrypt setting
        if (info.Properties.TryGetValue("Encrypt", out var encryptValue))
        {
            if (string.Equals(encryptValue, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Default behavior for most providers is to encrypt when connecting to remote servers
        // For SQL Server, default is true for remote connections
        return true;
    }

    private int GetDefaultTlsPort(DbProvider provider)
    {
        return provider switch
        {
            DbProvider.SqlServer => 1433, // SQL Server default port
            DbProvider.PostgreSql => 5432, // PostgreSQL default port
            DbProvider.MySql => 3306, // MySQL default port
            _ => 0 // Unknown provider
        };
    }

    private sealed class TlsInspectionResult
    {
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();
    }
}