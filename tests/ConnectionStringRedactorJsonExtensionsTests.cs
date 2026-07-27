using Xunit;
using System.Text.Json;

namespace ConnStringDoctor.Tests;

public class ConnectionStringRedactorJsonExtensionsTests
{
    [Theory]
    [InlineData("Server=myServer;Password=mySecretPassword;User Id=myUser", "mySecretPassword")]
    [InlineData("Server=myServer;Pwd=mySecretPwd;User Id=myUser", "mySecretPwd")]
    [InlineData("Server=myServer;User=myUser;Token=mySecretToken", "mySecretToken")]
    [InlineData("Server=myServer;Secret=mySecretValue", "mySecretValue")]
    [InlineData("Server=myServer;Passwd=mySecretPasswd", "mySecretPasswd")] // MySQL alias
    public void RedactToJson_DoesNotLeakSecrets(string connectionString, string secret)
    {
        var json = connectionString.RedactToJson();
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.False(root.TryGetProperty("original", out _), "JSON should not contain Original");
        Assert.True(root.TryGetProperty("redacted", out var redacted), "JSON should contain Redacted");
        Assert.DoesNotContain(secret, redacted.GetString());
    }

    [Fact]
    public void ContainsSecretsToJson_DoesNotLeakSecrets()
    {
        var connectionString = "Server=myServer;Password=mySecretPassword";
        
        var json = connectionString.ContainsSecretsToJson();
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.True(root.TryGetProperty("connectionString", out var redactedCs));
        Assert.DoesNotContain("mySecretPassword", redactedCs.GetString());
        Assert.True(root.TryGetProperty("hasSecrets", out var hasSecrets));
        Assert.True(hasSecrets.GetBoolean());
    }
}
