using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// xUnit tests for <see cref="ConnectionStringRedactor"/>.
/// </summary>
public class ConnectionStringRedactorTests
{
    [Fact]
    public void Redact_DefaultMode_MasksPasswordAndToken()
    {
        var connectionString = "Server=myServer;Password=myPassword;User Id=myUser;Token=abc123";

        var resultFull = ConnectionStringRedactor.Redact(connectionString);

        Assert.DoesNotContain("myPassword", resultFull);
        Assert.DoesNotContain("abc123", resultFull);
    }

    [Fact]
    public void Redact_ExplicitFullMode_MatchesDefaultMode()
    {
        var connectionString = "Server=myServer;Password=myPassword;User Id=myUser;Token=abc123";

        var resultFull = ConnectionStringRedactor.Redact(connectionString);
        var resultExplicit = ConnectionStringRedactor.Redact(connectionString, RedactionMode.Full);

        Assert.Equal(resultFull, resultExplicit);
    }

    [Fact]
    public void Redact_PartialMode_MasksSecretsButPreservesNonSecrets()
    {
        var connectionString = "Server=myServer;Password=myPassword;User Id=myUser;Token=abc123";

        var result = ConnectionStringRedactor.Redact(connectionString, RedactionMode.Partial);

        Assert.DoesNotContain("myPassword", result);
        Assert.DoesNotContain("myUser", result);
        Assert.DoesNotContain("abc123", result);
        Assert.Contains("Server=myServer", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("my****rd", result);
        Assert.Contains("ab****23", result);
    }

    [Fact]
    public void RedactToDictionary_PartialMode_MasksPasswordAndPreservesServer()
    {
        var connectionString = "Server=myServer;Password=myPassword;User Id=myUser";

        var result = ConnectionStringRedactor.RedactToDictionary(connectionString, RedactionMode.Partial);

        Assert.NotEqual("myPassword", result.GetValueOrDefault("password") ?? result.GetValueOrDefault("Password"));
        var redactedPassword = result.GetValueOrDefault("password") ?? result.GetValueOrDefault("Password");
        Assert.Equal("my****rd", redactedPassword);
        Assert.True(result.GetValueOrDefault("server") == "myServer" || result.GetValueOrDefault("Server") == "myServer");
        var redactedUserId = result.GetValueOrDefault("user id") ?? result.GetValueOrDefault("User Id");
        Assert.Equal("my****er", redactedUserId);
    }

    [Fact]
    public void RedactKeepUser_MasksPasswordButKeepsUserAndServer()
    {
        var connectionString = "Server=myServer;Password=myPassword;User Id=myUser";

        var result = ConnectionStringRedactor.RedactKeepUser(connectionString);

        Assert.DoesNotContain("myPassword", result);
        Assert.Contains("myUser", result);
        Assert.DoesNotContain("Password", result);
        Assert.Contains("Server", result);
    }

    [Fact]
    public void ContainsSecrets_DetectsPasswordKeyword() =>
        Assert.True(ConnectionStringRedactor.ContainsSecrets("Server=myServer;Password=myPassword"));

    [Fact]
    public void ContainsSecrets_ReturnsFalseWhenNoSecretKeywords() =>
        Assert.False(ConnectionStringRedactor.ContainsSecrets("Server=myServer;Database=mydb"));

    [Fact]
    public void ContainsSecrets_ReturnsFalseForNull() =>
        Assert.False(ConnectionStringRedactor.ContainsSecrets(null));

    [Fact]
    public void ContainsSecrets_ReturnsFalseForEmptyString() =>
        Assert.False(ConnectionStringRedactor.ContainsSecrets(""));
}
