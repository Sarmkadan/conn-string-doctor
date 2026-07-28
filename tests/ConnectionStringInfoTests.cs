using System.Collections.Generic;
using Xunit;
using ConnStringDoctor;

namespace ConnStringDoctor.Tests;

public class ConnectionStringInfoTests
{
    [Fact]
    public void ToString_WithDefaultValues_ReturnsProviderOnly()
    {
        var info = new ConnectionStringInfo();

        // Default Provider is Unknown, everything else is null/empty
        var result = info.ToString();

        Assert.Equal("Provider: Unknown", result);
    }

    [Fact]
    public void ToString_WithAllFields_IncludesExpectedSegments()
    {
        var info = new ConnectionStringInfo
        {
            Provider = DbProvider.SqlServer,
            Server = "myHost",
            Port = 1433,
            Database = "myDb",
            User = "admin",
            Password = "secret"
        };
        info.Properties["Extra"] = "value";

        var result = info.ToString();

        // Verify each expected part appears (order is defined by implementation)
        Assert.Contains("Provider: SqlServer", result);
        Assert.Contains("Server: myHost:1433", result);
        Assert.Contains("Database: myDb", result);
        Assert.Contains("User: admin", result);
        // Password is deliberately omitted
        Assert.DoesNotContain("secret", result);
        // Properties count should be reported
        Assert.Contains("Properties: 1", result);
    }

    [Fact]
    public void Properties_Dictionary_IsCaseInsensitive()
    {
        var info = new ConnectionStringInfo();

        // Add using mixed case
        info.Properties["MyKey"] = "value1";

        // Retrieve using different case
        Assert.True(info.Properties.TryGetValue("mykey", out var retrieved));
        Assert.Equal("value1", retrieved);
    }

    [Fact]
    public void Server_EmptyString_NotIncludedInToString()
    {
        var info = new ConnectionStringInfo
        {
            Provider = DbProvider.PostgreSql,
            Server = string.Empty,
            Database = "db"
        };

        var result = info.ToString();

        // Server part should be omitted because it's empty
        Assert.DoesNotContain("Server:", result);
        // Database part should still be present
        Assert.Contains("Database: db", result);
    }

    [Fact]
    public void AddingMultipleProperties_PropertiesCountReflectedInToString()
    {
        var info = new ConnectionStringInfo
        {
            Provider = DbProvider.MySql
        };
        info.Properties["A"] = "1";
        info.Properties["B"] = "2";
        info.Properties["C"] = "3";

        var result = info.ToString();

        Assert.Contains("Properties: 3", result);
    }
}
