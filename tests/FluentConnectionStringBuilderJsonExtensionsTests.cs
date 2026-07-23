using Xunit;

namespace ConnStringDoctor;

/// <summary>
/// xUnit tests for FluentConnectionStringBuilderJsonExtensions JSON serialization/deserialization.
/// </summary>
public class FluentConnectionStringBuilderJsonExtensionsTests
{
    [Fact]
    public void ToJson_SqlServerBuilder_ProducesValidJson()
    {
        // Arrange
        var builder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb")
            .WithCredentials("user", "pass")
            .WithTimeout(30);

        // Act
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(builder);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"provider\":\"sqlserver\"", json);
        Assert.Contains("\"host\":\"localhost\"", json);
        Assert.Contains("\"database\":\"testdb\"", json);
        Assert.Contains("\"user\":\"user\"", json);
        Assert.Contains("\"password\":\"pass\"", json);
        Assert.Contains("\"timeout\":30", json);
    }

    [Fact]
    public void ToJson_WithIndented_ReturnsFormattedJson()
    {
        // Arrange
        var builder = FluentConnectionStringBuilder.For("postgresql")
            .WithHost("db.example.com")
            .WithDatabase("mydb");

        // Act
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(builder, indented: true);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("{\n", json);
        Assert.Contains("\n", json);
        Assert.Contains("  \"provider\":", json);
    }

    [Fact]
    public void ToJson_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        FluentConnectionStringBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FluentConnectionStringBuilderJsonExtensions.ToJson(builder!));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsBuilderInstance()
    {
        // Arrange
        var originalBuilder = FluentConnectionStringBuilder.For("mysql")
            .WithHost("localhost", 3306)
            .WithDatabase("mydb")
            .WithCredentials("user", "pass")
            .WithTimeout(60);

        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        string resultConnectionString = result.Build();
        Assert.Contains("Server=localhost", resultConnectionString);
        Assert.Contains("Port=3306", resultConnectionString);
        Assert.Contains("Database=mydb", resultConnectionString);
        Assert.Contains("User Id=user", resultConnectionString);
        Assert.Contains("Password=pass", resultConnectionString);
        Assert.Contains("Connection Timeout=60", resultConnectionString);
    }

    [Fact]
    public void FromJson_EmptyJson_ReturnsNull()
    {
        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_WhitespaceJson_ReturnsNull()
    {
        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_NullJson_ReturnsNull()
    {
        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_InvalidJson_ReturnsNull()
    {
        // Arrange
        string invalidJson = "{ invalid json {{{";

        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson(invalidJson);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_JsonWithoutProvider_ReturnsNull()
    {
        // Arrange
        string jsonWithoutProvider = "{\"host\":\"localhost\"}";

        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson(jsonWithoutProvider);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_JsonWithEmptyProvider_ReturnsNull()
    {
        // Arrange
        string jsonWithEmptyProvider = "{\"provider\":\"\"}";

        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson(jsonWithEmptyProvider);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndBuilder()
    {
        // Arrange
        var originalBuilder = FluentConnectionStringBuilder.For("sqlite")
            .WithDatabase("/path/to/db.sqlite");

        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act
        bool success = FluentConnectionStringBuilderJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        string resultConnectionString = result!.Build();
        Assert.Contains("Data Source=/path/to/db.sqlite", resultConnectionString);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        string invalidJson = "{ invalid";

        // Act
        bool success = FluentConnectionStringBuilderJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_EmptyJson_ReturnsFalseAndNull()
    {
        // Act
        bool success = FluentConnectionStringBuilderJsonExtensions.TryFromJson("", out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Roundtrip_SqlServerConfiguration_PreservesAllProperties()
    {
        // Arrange - create a complex builder
        var originalBuilder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("db.example.com", 1433)
            .WithDatabase("ProductionDB")
            .WithCredentials("admin", "P@ssw0rd!")
            .WithTimeout(60)
            .WithPooling(10, 100)
            .WithSsl(false)
            .WithOption("Application Name", "MyApp")
            .WithOption("MultipleActiveResultSets", "true");

        // Act - serialize to JSON
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act - deserialize back
        var deserializedBuilder = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedBuilder);
        string result = deserializedBuilder.Build();

        Assert.Contains("Server=db.example.com,1433", result);
        Assert.Contains("Database=ProductionDB", result);
        Assert.Contains("User Id=admin", result);
        Assert.Contains("Password=P@ssw0rd!", result);
        Assert.Contains("Connection Timeout=60", result);
        Assert.Contains("Pooling=True", result);
        Assert.Contains("Minimum Pool Size=10", result);
        Assert.Contains("Maximum Pool Size=100", result);
        Assert.Contains("Encrypt=False", result);
        Assert.Contains("Application Name=MyApp", result);
        Assert.Contains("MultipleActiveResultSets=true", result);
    }

    [Fact]
    public void Roundtrip_PostgreSqlConfiguration_PreservesAllProperties()
    {
        // Arrange - PostgreSQL specific configuration
        var originalBuilder = FluentConnectionStringBuilder.For("postgresql")
            .WithHost("pg.example.com")
            .WithDatabase("testdb")
            .WithCredentials("postgres", "secret")
            .WithTimeout(30)
            .WithSsl(true);

        // Act - serialize to JSON
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act - deserialize back
        var deserializedBuilder = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedBuilder);
        string result = deserializedBuilder.Build();

        Assert.Contains("Host=pg.example.com", result);
        Assert.Contains("Database=testdb", result);
        Assert.Contains("Username=postgres", result);
        Assert.Contains("Password=secret", result);
        Assert.Contains("Timeout=30", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void Roundtrip_MySqlConfiguration_PreservesAllProperties()
    {
        // Arrange - MySQL specific configuration
        var originalBuilder = FluentConnectionStringBuilder.For("mysql")
            .WithHost("mysql.example.com", 3306)
            .WithDatabase("mydb")
            .WithIntegratedSecurity()
            .WithTimeout(45);

        // Act - serialize to JSON
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act - deserialize back
        var deserializedBuilder = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedBuilder);
        string result = deserializedBuilder.Build();

        Assert.Contains("Server=mysql.example.com", result);
        Assert.Contains("Port=3306", result);
        Assert.Contains("Database=mydb", result);
        Assert.Contains("IntegratedSecurity=true", result);
        Assert.Contains("Connection Timeout=45", result);
    }

    [Fact]
    public void Roundtrip_SqliteConfiguration_PreservesAllProperties()
    {
        // Arrange - SQLite specific configuration
        var originalBuilder = FluentConnectionStringBuilder.For("sqlite")
            .WithDatabase("/var/lib/sqlite/mydb.sqlite3")
            .WithTimeout(15);

        // Act - serialize to JSON
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act - deserialize back
        var deserializedBuilder = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedBuilder);
        string result = deserializedBuilder.Build();

        Assert.Contains("Data Source=/var/lib/sqlite/mydb.sqlite3", result);
        Assert.Contains("Connection Timeout=15", result);
    }

    [Fact]
    public void Roundtrip_WithIntegratedSecurity_PreservesSecuritySetting()
    {
        // Arrange
        var originalBuilder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb")
            .WithIntegratedSecurity()
            .WithTimeout(30);

        // Act
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);
        var deserializedBuilder = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedBuilder);
        string result = deserializedBuilder.Build();

        Assert.Contains("Integrated Security=True", result);
        Assert.DoesNotContain("User Id=", result);
        Assert.DoesNotContain("Password=", result);
    }

    [Fact]
    public void TryFromJson_WithNullOutputParameter_WorksCorrectly()
    {
        // Arrange
        var originalBuilder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb");

        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act
        bool success = FluentConnectionStringBuilderJsonExtensions.TryFromJson(json, out FluentConnectionStringBuilder? result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
    }

    [Fact]
    public void ToJson_UsesCamelCasePropertyNames()
    {
        // Arrange
        var builder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb");

        // Act
        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(builder);

        // Assert - should use camelCase for JSON properties
        Assert.Contains("\"provider\"", json);
        Assert.Contains("\"host\"", json);
        Assert.Contains("\"database\"", json);
        Assert.DoesNotContain("\"Provider\"", json);
        Assert.DoesNotContain("\"Host\"", json);
        Assert.DoesNotContain("\"Database\"", json);
    }

    [Fact]
    public void FromJson_WithCustomOptions_PreservesOptions()
    {
        // Arrange
        var originalBuilder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb")
            .WithOption("CustomKey1", "CustomValue1")
            .WithOption("CustomKey2", "CustomValue2");

        string json = FluentConnectionStringBuilderJsonExtensions.ToJson(originalBuilder);

        // Act
        var result = FluentConnectionStringBuilderJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        string resultConnectionString = result.Build();
        Assert.Contains("CustomKey1=CustomValue1", resultConnectionString);
        Assert.Contains("CustomKey2=CustomValue2", resultConnectionString);
    }
}
