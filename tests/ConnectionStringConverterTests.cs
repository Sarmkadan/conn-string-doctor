using Xunit;
using ConnStringDoctor;

namespace ConnStringDoctor.Tests;

public class ConnectionStringConverterTests
{
    [Fact]
    public void Parse_ValidString_ReturnsParts()
    {
        var cs = "Server=myServer;Database=myDb;";
        var info = ConnectionStringConverter.Parse(cs);
        Assert.Equal("myServer", info.OriginalParts["Server"]);
        Assert.Equal("myDb", info.OriginalParts["Database"]);
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var cs = "Server=myServer;";
        bool success = ConnectionStringConverter.TryParse(cs, out var info);
        Assert.True(success);
        Assert.NotNull(info);
    }

    [Fact]
    public void Convert_SqlServerToPostgres_ConvertsSuccessfully()
    {
        var converter = new ConnectionStringConverter();
        var info = new ConnectionStringConverter.ConnectionStringInfo
        {
            Provider = "sqlserver",
            OriginalParts = new Dictionary<string, string> { { "Server", "myHost" }, { "Database", "myDb" } }
        };

        var result = converter.Convert(info, "postgres");

        Assert.Contains("Host=myHost", result.ConnectionString);
        Assert.Contains("Database=myDb", result.ConnectionString);
    }

    [Fact]
    public void Convert_UnmappedKeys_PopulatedWhenNotMapped()
    {
        var converter = new ConnectionStringConverter();
        var info = new ConnectionStringConverter.ConnectionStringInfo
        {
            Provider = "sqlserver",
            OriginalParts = new Dictionary<string, string> { { "UnknownKey", "Value" } }
        };

        var result = converter.Convert(info, "postgres");

        Assert.NotEmpty(result.UnmappedKeys);
        Assert.Equal("UnknownKey", result.UnmappedKeys[0]);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void Convert_NullSource_ThrowsArgumentNullException()
    {
        var converter = new ConnectionStringConverter();
        Assert.Throws<ArgumentNullException>(() => converter.Convert(null!, "postgres"));
    }

    [Fact]
    public void Convert_NullOrEmptyTargetProvider_ThrowsArgumentException()
    {
        var converter = new ConnectionStringConverter();
        var info = new ConnectionStringConverter.ConnectionStringInfo();
        Assert.Throws<ArgumentException>(() => converter.Convert(info, null!));
        Assert.Throws<ArgumentException>(() => converter.Convert(info, ""));
    }
}
