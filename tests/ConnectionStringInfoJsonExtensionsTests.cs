using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using System;

namespace ConnStringDoctor
{
    public class ConnectionStringInfoJsonExtensionsTests
    {
        [Fact]
        public void ToJson_Happy_PATH()
        {
            // Arrange
            var connectionStringInfo = new ConnectionStringInfo();
            // Act
            var json = ConnectionStringInfoJsonExtensions.ToJson(connectionStringInfo);
            // Assert
            Assert.NotNull(json);
        }

        [Fact]
        public void FromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{}";
            // Act
            var connectionStringInfo = ConnectionStringInfoJsonExtensions.FromJson(json);
            // Assert
            Assert.NotNull(connectionStringInfo);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{}";
            ConnectionStringInfo? connectionStringInfo;
            // Act
            var result = ConnectionStringInfoJsonExtensions.TryFromJson(json, out connectionStringInfo);
            // Assert
            Assert.True(result);
            Assert.NotNull(connectionStringInfo);
        }
    }
}