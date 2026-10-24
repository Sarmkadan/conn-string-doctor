using Xunit;
using ConnStringDoctor;

namespace ConnStringDoctor.Tests;

public class ConnectionStringConverterExtensionsTests
{
    private static ConnectionStringConverter.ConnectionStringInfo InfoWith(params (string Key, string Value)[] pairs)
    {
        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            parts[key] = value;
        }

        return new ConnectionStringConverter.ConnectionStringInfo
        {
            Provider = "sqlserver",
            OriginalParts = parts
        };
    }

    [Fact]
    public void TryConvert_ValidSourceAndTarget_ReturnsTrueWithResult()
    {
        var converter = new ConnectionStringConverter();
        var source = InfoWith(("Server", "db.local"), ("Database", "orders"));

        var ok = converter.TryConvert(source, " postgres ", out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Contains("db.local", result!.ConnectionString);
    }

    [Fact]
    public void TryConvert_InvalidArguments_ThrowsOrReturnsFalse()
    {
        var converter = new ConnectionStringConverter();
        var source = InfoWith(("Server", "db.local"));

        Assert.Throws<ArgumentNullException>(() => converter.TryConvert(null!, "postgres", out _));
        Assert.Throws<ArgumentNullException>(() => ((ConnectionStringConverter)null!).TryConvert(source, "postgres", out _));
        Assert.Throws<ArgumentException>(() => converter.TryConvert(source, "", out _));

        // Whitespace passes the extension's null/empty guard, then the converter rejects it inside the try block.
        var ok = converter.TryConvert(source, "   ", out var result);
        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_KeyLookup_IsCaseInsensitiveAndNullWhenMissing()
    {
        var info = InfoWith(("Server", "db.local"));

        Assert.Equal("db.local", info.GetValue("server"));
        Assert.Null(info.GetValue("Port"));
        Assert.Throws<ArgumentNullException>(() => ((ConnectionStringConverter.ConnectionStringInfo)null!).GetValue("Server"));
        Assert.Throws<ArgumentException>(() => info.GetValue(""));
    }

    [Fact]
    public void WarningAndUnmappedHelpers_EmptyAndPopulatedResults_ReportCorrectly()
    {
        var clean = new ConversionResult("Host=h", Array.Empty<string>(), Array.Empty<string>());
        Assert.False(clean.HasWarnings());
        Assert.False(clean.HasUnmappedKeys());
        Assert.Null(clean.FirstWarning());
        Assert.Null(clean.FirstUnmappedKey());

        var noisy = new ConversionResult("Host=h", new[] { "Pooling", "Encrypt" }, new[] { "first warning", "second warning" });
        Assert.True(noisy.HasWarnings());
        Assert.True(noisy.HasUnmappedKeys());
        Assert.Equal("first warning", noisy.FirstWarning());
        Assert.Equal("Pooling", noisy.FirstUnmappedKey());

        Assert.Throws<ArgumentNullException>(() => ((ConversionResult)null!).HasWarnings());
        Assert.Throws<ArgumentNullException>(() => ((ConversionResult)null!).FirstUnmappedKey());
    }

    [Fact]
    public void ParseAndConvert_UnknownSourceProvider_ReportsUnmappedKeys()
    {
        var converter = new ConnectionStringConverter();

        var result = converter.ParseAndConvert("Server=db.local;Database=orders", "postgres");

        Assert.True(result.HasUnmappedKeys());
        Assert.True(result.HasWarnings());
        Assert.NotNull(result.FirstUnmappedKey());

        Assert.Throws<ArgumentNullException>(() => ((ConnectionStringConverter)null!).ParseAndConvert("Server=x", "postgres"));
        Assert.Throws<ArgumentException>(() => converter.ParseAndConvert("", "postgres"));
        Assert.Throws<ArgumentException>(() => converter.ParseAndConvert("Server=x", ""));
    }

    [Fact]
    public void GetDatabaseName_DatabaseThenInitialCatalogFallback_ReturnsFirstNonBlank()
    {
        Assert.Equal("orders", InfoWith(("Database", "orders")).GetDatabaseName());
        Assert.Equal("sales", InfoWith(("Initial Catalog", "sales")).GetDatabaseName());
        Assert.Equal("sales", InfoWith(("Database", "  "), ("Initial Catalog", "sales")).GetDatabaseName());
        Assert.Equal("orders", InfoWith(("Database", "orders"), ("Initial Catalog", "sales")).GetDatabaseName());
        Assert.Null(InfoWith(("Server", "db.local")).GetDatabaseName());
        Assert.Throws<ArgumentNullException>(() => ((ConnectionStringConverter.ConnectionStringInfo)null!).GetDatabaseName());
    }

    [Fact]
    public void GetServerName_ServerHostDataSourcePrecedence_ReturnsFirstNonBlank()
    {
        Assert.Equal("a", InfoWith(("Server", "a"), ("Host", "b"), ("Data Source", "c")).GetServerName());
        Assert.Equal("b", InfoWith(("Server", " "), ("Host", "b"), ("Data Source", "c")).GetServerName());
        Assert.Equal("c", InfoWith(("Data Source", "c")).GetServerName());
        Assert.Null(InfoWith(("Database", "orders")).GetServerName());
        Assert.Throws<ArgumentNullException>(() => ((ConnectionStringConverter.ConnectionStringInfo)null!).GetServerName());
    }

    [Fact]
    public void GetPort_BoundaryAndInvalidValues_ReturnsOnlyValidPorts()
    {
        Assert.Equal(5432, InfoWith(("Port", "5432")).GetPort());
        Assert.Equal(1, InfoWith(("Port", "1")).GetPort());
        Assert.Equal(65535, InfoWith(("Port", "65535")).GetPort());
        Assert.Null(InfoWith(("Port", "0")).GetPort());
        Assert.Null(InfoWith(("Port", "65536")).GetPort());
        Assert.Null(InfoWith(("Port", "-1")).GetPort());
        Assert.Null(InfoWith(("Port", "abc")).GetPort());
        Assert.Null(InfoWith(("Port", " ")).GetPort());
        Assert.Null(InfoWith(("Server", "db.local")).GetPort());
        Assert.Throws<ArgumentNullException>(() => ((ConnectionStringConverter.ConnectionStringInfo)null!).GetPort());
    }
}
