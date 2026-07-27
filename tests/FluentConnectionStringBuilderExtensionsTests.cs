using System;
using System.Collections.Generic;
using Xunit;
using ConnStringDoctor;

namespace ConnStringDoctor.Tests;

public class FluentConnectionStringBuilderExtensionsTests
{
    [Fact]
    public void WithOptions_AddsOptionsToBuilder()
    {
        var builder = FluentConnectionStringBuilder.For("generic");
        var options = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };
        
        builder.WithOptions(options);
        
        var connectionString = builder.Build();
        Assert.Contains("Key1=Value1", connectionString);
        Assert.Contains("Key2=Value2", connectionString);
    }

    [Fact]
    public void WithOptions_ThrowsException_OnNullBuilder()
    {
        FluentConnectionStringBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.WithOptions(new Dictionary<string, string>()));
    }

    [Fact]
    public void WithOptions_ThrowsException_OnNullOptions()
    {
        var builder = FluentConnectionStringBuilder.For("generic");
        Assert.Throws<ArgumentNullException>(() => builder.WithOptions(null!));
    }

    [Fact]
    public void WithTimeout_SetsTimeout_WhenValid()
    {
        var builder = FluentConnectionStringBuilder.For("generic");
        var timeout = TimeSpan.FromSeconds(30);
        
        builder.WithTimeout(timeout);
        
        var connectionString = builder.Build();
        Assert.Contains("timeout=30", connectionString);
    }

    [Fact]
    public void WithTimeout_ThrowsException_WhenTimeoutIsZeroOrNegative()
    {
        var builder = FluentConnectionStringBuilder.For("generic");
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void WithPoolSize_SetsPoolSize_WhenValid()
    {
        var builder = FluentConnectionStringBuilder.For("sqlserver");
        
        builder.WithPoolSize(10);
        
        var connectionString = builder.Build();
        Assert.Contains("Min Pool Size=10", connectionString);
        Assert.Contains("Max Pool Size=10", connectionString);
    }

    [Fact]
    public void WithPoolSize_ThrowsException_WhenPoolSizeIsZeroOrNegative()
    {
        var builder = FluentConnectionStringBuilder.For("sqlserver");
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithPoolSize(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithPoolSize(-5));
    }

    [Fact]
    public void WithDatabaseFromUri_SetsDatabase_WhenValid()
    {
        var builder = FluentConnectionStringBuilder.For("sqlserver");
        builder.WithHost("localhost");
        
        builder.WithDatabaseFromUri("Database=MyDatabase;Server=localhost");
        
        var connectionString = builder.Build();
        Assert.Contains("Database=MyDatabase", connectionString);
    }
}
