using Xunit;
namespace ConnStringDoctor
{
    public class FluentConnectionStringBuilderJsonExtensionsJsonExtensionsTests
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
    }
}
