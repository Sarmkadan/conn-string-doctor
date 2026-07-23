using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// xUnit tests for <see cref="ProviderDetector"/> provider detection heuristics.
/// </summary>
public class ProviderDetectorTests
{
    [Fact]
    public void DetectProvider_SqlServerWithIntegratedSecurity_ReturnsHighConfidenceSqlServer()
    {
        var result = ProviderDetector.DetectProvider("Server=localhost;Database=test;Integrated Security=True");

        Assert.Equal(DbProvider.SqlServer, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_SqlServerWithTrustedConnection_ReturnsHighConfidenceSqlServer()
    {
        var result = ProviderDetector.DetectProvider("Data Source=myServerAddress;Initial Catalog=myDataBase;Trusted_Connection=True;");

        Assert.Equal(DbProvider.SqlServer, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_PostgreSqlWithSslMode_ReturnsHighConfidencePostgreSql()
    {
        var result = ProviderDetector.DetectProvider("Host=localhost;Port=5432;Database=test;Username=user;Ssl Mode=Require;");

        Assert.Equal(DbProvider.PostgreSql, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_PostgreSqlWithDefaultPort_ReturnsHighConfidencePostgreSql()
    {
        var result = ProviderDetector.DetectProvider("Server=localhost;Port=5432;Database=test;");

        Assert.Equal(DbProvider.PostgreSql, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_MySqlWithDefaultPort_ReturnsHighConfidenceMySql()
    {
        var result = ProviderDetector.DetectProvider("Server=localhost;Port=3306;Database=test;");

        Assert.Equal(DbProvider.MySql, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_MySqlWithUserKeyword_ReturnsHighConfidenceMySql()
    {
        var result = ProviderDetector.DetectProvider("Host=localhost;User=root;Database=test;");

        Assert.Equal(DbProvider.MySql, result.Provider);
        Assert.True(result.Confidence > 80);
    }

    [Fact]
    public void DetectProvider_SqliteWithDbExtension_ReturnsHighConfidenceSqlite()
    {
        var result = ProviderDetector.DetectProvider("Data Source=/path/to/database.db;");

        Assert.Equal(DbProvider.Sqlite, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_SqliteWithSqliteExtension_ReturnsHighConfidenceSqlite()
    {
        var result = ProviderDetector.DetectProvider("Data Source=/path/to/database.sqlite;");

        Assert.Equal(DbProvider.Sqlite, result.Provider);
        Assert.True(result.Confidence > 90);
    }

    [Fact]
    public void DetectProvider_GenericConnectionString_DefaultsToSqlServer()
    {
        var result = ProviderDetector.DetectProvider("Server=localhost;Database=test;");

        Assert.Equal(DbProvider.SqlServer, result.Provider);
        Assert.True(result.Confidence > 70);
    }

    [Fact]
    public void DetectProvider_EmptyString_ReturnsUnknownWithZeroConfidence()
    {
        var result = ProviderDetector.DetectProvider("");

        Assert.Equal(DbProvider.Unknown, result.Provider);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public void DefaultPort_SqlServer_Returns1433() =>
        Assert.Equal(1433, ProviderDetector.DefaultPort(DbProvider.SqlServer));

    [Fact]
    public void DefaultPort_PostgreSql_Returns5432() =>
        Assert.Equal(5432, ProviderDetector.DefaultPort(DbProvider.PostgreSql));
}
