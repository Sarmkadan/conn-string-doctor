using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// Unit tests for <see cref="ConnectionStringInfoValidation"/>.
/// </summary>
public class ConnectionStringInfoValidationTests
{
    private static ConnectionStringInfo CreateValidInfo() => new()
    {
        Provider = DbProvider.SqlServer,
        Server = "db.example.com",
        Port = 1433,
        Database = "orders",
        User = "app",
        Password = "not-checked-here",
    };

    [Fact]
    public void Validate_CompleteInfo_ReturnsNoProblems()
    {
        var info = CreateValidInfo();
        info.Properties["Pooling"] = "true";

        Assert.Empty(ConnectionStringInfoValidation.Validate(info));
        Assert.True(ConnectionStringInfoValidation.IsValid(info));
        ConnectionStringInfoValidation.EnsureValid(info);
    }

    [Fact]
    public void Validate_EmptyInfo_ReportsRequiredFieldsInOrderAndSkipsOptionalPort()
    {
        var problems = ConnectionStringInfoValidation.Validate(new ConnectionStringInfo());

        Assert.Equal(
            new[]
            {
                "Provider is Unknown",
                "Server is null or empty",
                "Database is null or empty",
                "User is null or empty",
            },
            problems);
    }

    [Fact]
    public void Validate_WhitespaceServerAndDatabase_ReportsThemAsEmpty()
    {
        var info = CreateValidInfo();
        info.Server = "   ";
        info.Database = "\t";

        Assert.Equal(
            new[] { "Server is null or empty", "Database is null or empty" },
            ConnectionStringInfoValidation.Validate(info));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(65535, true)]
    [InlineData(0, false)]
    [InlineData(65536, false)]
    [InlineData(-1, false)]
    public void Validate_PortBoundaries_ReportsOnlyPortsOutsideValidRange(int port, bool expectedValid)
    {
        var info = CreateValidInfo();
        info.Port = port;

        var problems = ConnectionStringInfoValidation.Validate(info);

        if (expectedValid)
        {
            Assert.Empty(problems);
        }
        else
        {
            Assert.Equal(new[] { "Port is out of range (1-65535)" }, problems);
        }
    }

    [Fact]
    public void Validate_PropertyWithEmptyKeyOrNullValue_ReportsEachOffendingProperty()
    {
        var info = CreateValidInfo();
        info.Properties[string.Empty] = "orphan";
        info.Properties["Pooling"] = null;

        var problems = ConnectionStringInfoValidation.Validate(info);

        Assert.Equal(
            new[]
            {
                "Property key or value is null or empty: =orphan",
                "Property key or value is null or empty: Pooling=",
            },
            problems);
        Assert.False(ConnectionStringInfoValidation.IsValid(info));
    }

    [Fact]
    public void EnsureValid_InvalidInfo_ThrowsArgumentExceptionListingProblems()
    {
        var info = new ConnectionStringInfo { Provider = DbProvider.Unknown };

        var exception = Assert.Throws<ArgumentException>(() => ConnectionStringInfoValidation.EnsureValid(info));

        Assert.Equal("value", exception.ParamName);
        Assert.StartsWith("Invalid ConnectionStringInfo: Provider is Unknown, Server is null or empty", exception.Message);
        Assert.False(ConnectionStringInfoValidation.IsValid(info));
    }

    [Fact]
    public void AllMembers_NullInfo_ThrowArgumentNullExceptionForValueParameter()
    {
        ConnectionStringInfo? info = null;

        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => ConnectionStringInfoValidation.Validate(info!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => ConnectionStringInfoValidation.IsValid(info!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => ConnectionStringInfoValidation.EnsureValid(info!)).ParamName);
    }
}
