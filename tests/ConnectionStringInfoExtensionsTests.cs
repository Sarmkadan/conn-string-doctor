using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// Unit tests for <see cref="ConnectionStringInfoExtensions"/>.
/// </summary>
public class ConnectionStringInfoExtensionsTests
{
    [Fact]
    public void GetServerEndpoint_WithPositivePort_AppendsPort()
    {
        var info = new ConnectionStringInfo { Server = "db.example.com", Port = 1433 };

        Assert.Equal("db.example.com:1433", info.GetServerEndpoint());
    }

    [Fact]
    public void GetServerEndpoint_WithoutPositivePortOrServer_ReturnsServerOnlyOrEmpty()
    {
        Assert.Equal("db.example.com", new ConnectionStringInfo { Server = "db.example.com" }.GetServerEndpoint());
        Assert.Equal("db.example.com", new ConnectionStringInfo { Server = "db.example.com", Port = 0 }.GetServerEndpoint());
        Assert.Equal("db.example.com", new ConnectionStringInfo { Server = "db.example.com", Port = -1 }.GetServerEndpoint());
        Assert.Equal(string.Empty, new ConnectionStringInfo().GetServerEndpoint());
    }

    [Fact]
    public void AllMembers_NullInfo_ThrowArgumentNullExceptionForInfoParameter()
    {
        ConnectionStringInfo? info = null;

        Assert.Equal("info", Assert.Throws<ArgumentNullException>(() => info!.GetServerEndpoint()).ParamName);
        Assert.Equal("info", Assert.Throws<ArgumentNullException>(() => info!.IsLocalDatabase()).ParamName);
        Assert.Equal("info", Assert.Throws<ArgumentNullException>(() => info!.ToSanitizedString()).ParamName);
        Assert.Equal("info", Assert.Throws<ArgumentNullException>(() => info!.RequiresAuthentication()).ParamName);
    }

    [Fact]
    public void IsLocalDatabase_SqliteProvider_ReturnsTrueRegardlessOfServer()
    {
        var info = new ConnectionStringInfo { Provider = DbProvider.Sqlite, Server = "db.example.com" };

        Assert.True(info.IsLocalDatabase());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData(".")]
    public void IsLocalDatabase_SqlServerOnLoopbackOrNoServer_ReturnsTrue(string? server)
    {
        var info = new ConnectionStringInfo { Provider = DbProvider.SqlServer, Server = server };

        Assert.True(info.IsLocalDatabase());
    }

    [Theory]
    [InlineData(DbProvider.SqlServer, "db.example.com")]
    [InlineData(DbProvider.PostgreSql, "localhost")]
    [InlineData(DbProvider.MySql, "127.0.0.1")]
    [InlineData(DbProvider.Unknown, null)]
    public void IsLocalDatabase_RemoteServerOrNonSqlServerProvider_ReturnsFalse(DbProvider provider, string? server)
    {
        var info = new ConnectionStringInfo { Provider = provider, Server = server };

        Assert.False(info.IsLocalDatabase());
    }

    [Fact]
    public void ToSanitizedString_WithFullInfo_OmitsPasswordAndKeepsOtherParts()
    {
        var info = new ConnectionStringInfo
        {
            Provider = DbProvider.SqlServer,
            Server = "db.example.com",
            Port = 1433,
            Database = "app",
            User = "sa",
            Password = "test-password-placeholder",
        };
        info.Properties["Encrypt"] = "True";

        var result = info.ToSanitizedString();

        Assert.Equal("Provider=SqlServer;Server=db.example.com:1433;Database=app;User=sa;Encrypt=True", result);
        Assert.DoesNotContain("test-password-placeholder", result);
    }

    [Fact]
    public void ToSanitizedString_EmptyInfo_ReturnsProviderOnly()
    {
        Assert.Equal("Provider=Unknown", new ConnectionStringInfo().ToSanitizedString());
    }

    [Theory]
    [InlineData("sa", "test-password-placeholder", true)]
    [InlineData(null, "test-password-placeholder", false)]
    [InlineData("sa", null, false)]
    [InlineData("", "test-password-placeholder", false)]
    [InlineData("sa", "", false)]
    public void RequiresAuthentication_UserAndPasswordPresence_MatchesExpected(string? user, string? password, bool expected)
    {
        var info = new ConnectionStringInfo { User = user, Password = password };

        Assert.Equal(expected, info.RequiresAuthentication());
    }
}
