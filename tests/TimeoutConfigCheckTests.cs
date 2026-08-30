using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// xUnit tests for <see cref="TimeoutConfigCheck"/> diagnostic check.
/// </summary>
public class TimeoutConfigCheckTests
{
    private static DiagnosticResult RunCheck(string connectionString, CancellationToken ct = default)
    {
        var info = ConnectionStringParser.Parse(connectionString);
        var check = new TimeoutConfigCheck();
        return check.RunAsync(info, ct).Result;
    }

    [Fact]
    public void RunAsync_NullInfo_ThrowsArgumentNullException()
    {
        var check = new TimeoutConfigCheck();

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = check.RunAsync(null!, CancellationToken.None);
        });
    }

    [Fact]
    public void NoTimeoutSettings_WarnsAboutDefaultConnectTimeout()
    {
        var result = RunCheck("Server=localhost;");

        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Equal("Connect Timeout not specified; default is 15 seconds.", result.Warnings[0]);
        Assert.Equal(Severity.Warning, result.ResultSeverity);
    }

    [Theory]
    [InlineData("0", Severity.Info, null)]
    [InlineData("2147483647", Severity.Warning, "Connect Timeout is 2147483647 seconds")]
    [InlineData("not-a-number", Severity.Warning, "Connect Timeout not specified")]
    public void ConnectTimeout_BoundaryValues_ProduceExpectedSeverity(
        string timeout,
        Severity expectedSeverity,
        string? expectedWarning)
    {
        var result = RunCheck($"Server=localhost;Connect Timeout={timeout};");

        Assert.Empty(result.Errors);
        Assert.Equal(expectedSeverity, result.ResultSeverity);
        if (expectedWarning is null)
        {
            Assert.Empty(result.Warnings);
        }
        else
        {
            Assert.Single(result.Warnings);
            Assert.Contains(expectedWarning, result.Warnings[0]);
        }
    }

    [Theory]
    [InlineData("0", Severity.Warning, "Command Timeout is set to 0")]
    [InlineData("2147483647", Severity.Info, null)]
    [InlineData("not-a-number", Severity.Info, null)]
    public void CommandTimeout_BoundaryValues_ProduceExpectedSeverity(
        string timeout,
        Severity expectedSeverity,
        string? expectedWarning)
    {
        var result = RunCheck($"Server=localhost;Connect Timeout=15;Command Timeout={timeout};");

        Assert.Empty(result.Errors);
        Assert.Equal(expectedSeverity, result.ResultSeverity);
        if (expectedWarning is null)
        {
            Assert.Empty(result.Warnings);
        }
        else
        {
            Assert.Single(result.Warnings);
            Assert.Contains(expectedWarning, result.Warnings[0]);
        }
    }

    [Fact]
    public void RunAsync_PreCanceledToken_CompletesBecauseCancellationIsNotObserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = RunCheck("Server=localhost;Connect Timeout=15;Command Timeout=30;", cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(Severity.Info, result.ResultSeverity);
    }
}
