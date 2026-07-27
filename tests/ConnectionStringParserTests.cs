using Xunit;
using ConnStringDoctor;

namespace ConnStringDoctor.Tests;

public class ConnectionStringParserTests
{
    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenStringIsTooLong()
    {
        string longString = new string('a', 8193);
        Assert.Throws<ArgumentException>(() => ConnectionStringParser.Parse(longString));
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenTooManyPairs()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 257; i++)
        {
            sb.Append($"Key{i}=Value{i};");
        }
        string manyPairsString = sb.ToString();
        Assert.Throws<ArgumentException>(() => ConnectionStringParser.Parse(manyPairsString));
    }

    [Fact]
    public void Parse_ShouldSucceed_WhenExactlyMaximum()
    {
        var sb = new System.Text.StringBuilder();
        // Create 256 pairs. 8192 characters total is hard to match exactly with this logic, 
        // but let's ensure it doesn't throw for 256 pairs.
        for (int i = 0; i < 256; i++)
        {
            sb.Append($"K{i}=V;");
        }
        string validString = sb.ToString();
        var result = ConnectionStringParser.Parse(validString);
        Assert.NotNull(result);
        Assert.Equal(256, result.Properties.Count);
    }
}
