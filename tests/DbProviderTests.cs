using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// xUnit tests for <see cref="DbProviderMetadata"/> provider detection.
/// </summary>
public class DbProviderTests
{
    [Theory]
    [InlineData("sqlserver", DbProvider.SqlServer)]
    [InlineData("mssql", DbProvider.SqlServer)]
    [InlineData("postgresql", DbProvider.PostgreSql)]
    [InlineData("postgres", DbProvider.PostgreSql)]
    [InlineData("mysql", DbProvider.MySql)]
    [InlineData("sqlite", DbProvider.Sqlite)]
    public void TryParse_ValidName_ReturnsTrueAndCorrectProvider(string name, DbProvider expectedProvider)
    {
        bool result = DbProviderMetadata.TryParse(name, out DbProvider actualProvider);

        Assert.True(result);
        Assert.Equal(expectedProvider, actualProvider);
    }

    [Theory]
    [InlineData("oracle")]
    [InlineData("redis")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_InvalidName_ReturnsFalseAndUnknown(string? name)
    {
        bool result = DbProviderMetadata.TryParse(name, out DbProvider actualProvider);

        Assert.False(result);
        Assert.Equal(DbProvider.Unknown, actualProvider);
    }

    [Fact]
    public void GetName_SqlServer_ReturnsSqlServer()
    {
        string name = DbProviderMetadata.GetName(DbProvider.SqlServer);
        Assert.Equal("sqlserver", name);
    }

    [Fact]
    public void GetName_Unknown_ReturnsUnknown()
    {
        string name = DbProviderMetadata.GetName(DbProvider.Unknown);
        Assert.Equal("unknown", name);
    }
}
