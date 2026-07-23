using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// xUnit tests for <see cref="PoolConfigCheck"/> diagnostic check.
/// </summary>
public class PoolConfigCheckTests
{
    private static DiagnosticResult RunCheck(string connectionString)
    {
        var info = ConnectionStringParser.Parse(connectionString);
        var check = new PoolConfigCheck();
        return check.RunAsync(info, CancellationToken.None).Result;
    }

    [Fact]
    public void PoolingDisabled_WithBooleanFalse_WarnsAboutPooling()
    {
        var result = RunCheck("Server=localhost;Pooling=false;");

        Assert.Empty(result.Errors);
        Assert.Equal(3, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("Pooling is disabled"));
    }

    [Fact]
    public void PoolingDisabled_WithZeroValue_WarnsAboutPooling()
    {
        var result = RunCheck("Server=localhost;Pooling=0;");

        Assert.Empty(result.Errors);
        Assert.Equal(3, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("Pooling is disabled"));
    }

    [Fact]
    public void MaxPoolSize_AboveFiveHundred_WarnsAboutLargeSize()
    {
        var result = RunCheck("Server=localhost;Max Pool Size=600;");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("Max Pool Size is 600"));
        Assert.Contains(result.Warnings, w => w.Contains("Connect Timeout not specified"));
    }

    [Fact]
    public void MaxPoolSize_NotSpecified_WarnsAboutDefault()
    {
        var result = RunCheck("Server=localhost;");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Max Pool Size not specified", result.Warnings[0]);
        Assert.Contains("Connect Timeout not specified", result.Warnings[1]);
    }

    [Fact]
    public void MinPoolSize_GreaterThanMaxPoolSize_ReportsError()
    {
        var result = RunCheck("Server=localhost;Min Pool Size=200;Max Pool Size=100;");

        Assert.Single(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Min Pool Size (200) is greater than Max Pool Size (100)", result.Errors[0]);
        Assert.Contains("Connect Timeout not specified", result.Warnings[0]);
    }

    [Fact]
    public void MinPoolSize_SpecifiedWithoutMaxPoolSize_WarnsAboutDefault()
    {
        var result = RunCheck("Server=localhost;Min Pool Size=50;");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Max Pool Size not specified", result.Warnings[0]);
        Assert.Contains("Connect Timeout not specified", result.Warnings[1]);
    }

    [Fact]
    public void ConnectTimeout_AboveThirtySeconds_WarnsAboutLargeTimeout()
    {
        var result = RunCheck("Server=localhost;Connect Timeout=60;");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Max Pool Size not specified", result.Warnings[0]);
        Assert.Contains("Connect Timeout is 60 seconds", result.Warnings[1]);
    }

    [Fact]
    public void ConnectTimeout_NotSpecified_WarnsAboutDefault()
    {
        var result = RunCheck("Server=localhost;Max Pool Size=100;");

        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Connect Timeout not specified", result.Warnings[0]);
    }

    [Fact]
    public void MaxPoolSize_ExtremeValue_WarnsAboutLargeSize()
    {
        var result = RunCheck("Server=localhost;Max Pool Size=10000;");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Max Pool Size is 10000", result.Warnings[0]);
        Assert.Contains("Connect Timeout not specified", result.Warnings[1]);
    }

    [Fact]
    public void MinPoolSize_Zero_WithValidMaxPoolSize_OnlyWarnsAboutTimeout()
    {
        var result = RunCheck("Server=localhost;Min Pool Size=0;Max Pool Size=100;");

        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Connect Timeout not specified", result.Warnings[0]);
    }

    [Fact]
    public void Pooling_EnabledExplicitly_OnlyWarnsAboutTimeout()
    {
        var result = RunCheck("Server=localhost;Pooling=true;Max Pool Size=200;");

        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Connect Timeout not specified", result.Warnings[0]);
    }

    [Fact]
    public void MaxPoolSize_MaximumPoolSizeSynonym_WarnsAboutLargeSize()
    {
        var result = RunCheck("Server=localhost;Maximum Pool Size=700;");

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Max Pool Size is 700", result.Warnings[0]);
        Assert.Contains("Connect Timeout not specified", result.Warnings[1]);
    }

    [Fact]
    public void MinPoolSize_MinimumPoolSizeSynonym_OnlyWarnsAboutTimeout()
    {
        var result = RunCheck("Server=localhost;Minimum Pool Size=50;Max Pool Size=100;");

        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Connect Timeout not specified", result.Warnings[0]);
    }

    [Fact]
    public void MinPoolSize_EqualToMaxPoolSize_IsValid()
    {
        var result = RunCheck("Server=localhost;Min Pool Size=100;Max Pool Size=100;");

        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Connect Timeout not specified", result.Warnings[0]);
    }
}
