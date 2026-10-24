using Xunit;

namespace ConnStringDoctor.Tests;

public class FluentConnectionStringBuilderValidationTests
{
    [Fact]
    public void Validate_ReturnsEmptyList_ForCompleteCredentialBuilder()
    {
        var builder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost", 1433)
            .WithDatabase("app")
            .WithCredentials("sa", "Test-Only-Pw1");

        var problems = FluentConnectionStringBuilderValidation.Validate(builder);

        Assert.Empty(problems);
        Assert.True(FluentConnectionStringBuilderValidation.IsValid(builder));
    }

    [Fact]
    public void Validate_ReportsMissingFieldsInOrder_ForFreshGenericBuilder()
    {
        var builder = FluentConnectionStringBuilder.For("generic");

        var problems = FluentConnectionStringBuilderValidation.Validate(builder);

        Assert.Equal(
            new[]
            {
                "Server is null or empty",
                "Database is null or empty",
                "User is null or empty",
                "Password is null or empty",
            },
            problems);
        Assert.False(FluentConnectionStringBuilderValidation.IsValid(builder));
    }

    [Fact]
    public void Validate_DoesNotRequireDatabaseOrCredentials_WhenIntegratedSecurityEnabled()
    {
        var builder = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithIntegratedSecurity();

        Assert.Empty(FluentConnectionStringBuilderValidation.Validate(builder));
        Assert.True(FluentConnectionStringBuilderValidation.IsValid(builder));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(65535, true)]
    [InlineData(65536, false)]
    public void Validate_ChecksPortRangeAtBoundaries(int port, bool expectedValid)
    {
        var builder = FluentConnectionStringBuilder.For("postgresql")
            .WithHost("db.internal", port)
            .WithDatabase("app")
            .WithCredentials("app_user", "Test-Only-Pw1");

        var problems = FluentConnectionStringBuilderValidation.Validate(builder);

        Assert.Equal(expectedValid, problems.Count == 0);
        Assert.Equal(expectedValid, FluentConnectionStringBuilderValidation.IsValid(builder));
        if (!expectedValid)
        {
            Assert.Equal(new[] { "Port is out of range (1-65535)" }, problems);
        }
    }

    [Fact]
    public void EnsureValid_DoesNotThrow_ForValidBuilder()
    {
        var builder = FluentConnectionStringBuilder.For("mysql")
            .WithHost("localhost")
            .WithDatabase("app")
            .WithCredentials("root", "Test-Only-Pw1");

        var exception = Record.Exception(() => FluentConnectionStringBuilderValidation.EnsureValid(builder));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_ListingProblems_ForInvalidBuilder()
    {
        var builder = FluentConnectionStringBuilder.For("generic");

        var ex = Assert.Throws<ArgumentException>(() => FluentConnectionStringBuilderValidation.EnsureValid(builder));

        Assert.Equal("value", ex.ParamName);
        Assert.StartsWith("Invalid FluentConnectionStringBuilder: Server is null or empty, ", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_IsValid_EnsureValid_ThrowArgumentNullException_ForNullBuilder()
    {
        FluentConnectionStringBuilder? builder = null;

        var validateEx = Assert.Throws<ArgumentNullException>(() => FluentConnectionStringBuilderValidation.Validate(builder!));
        var isValidEx = Assert.Throws<ArgumentNullException>(() => FluentConnectionStringBuilderValidation.IsValid(builder!));
        var ensureEx = Assert.Throws<ArgumentNullException>(() => FluentConnectionStringBuilderValidation.EnsureValid(builder!));

        Assert.Equal("value", validateEx.ParamName);
        Assert.Equal("value", isValidEx.ParamName);
        Assert.Equal("value", ensureEx.ParamName);
    }
}
