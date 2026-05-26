using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoSourceDescriptionTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        // Arrange
        var sourceDescription = new ArazzoSourceDescription
        {
            Name = "Test Source",
            Url = new Uri("https://example.com/api"),
            Type = ArazzoDescriptionType.OpenAPI
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
"""
{
    "name": "Test Source",
    "url": "https://example.com/api",
    "type": "openapi"
}
""";

        // Act
        sourceDescription.SerializeAsV1(writer);
        var jsonResult = textWriter.ToString();
        var jsonResultObject = JsonNode.Parse(jsonResult);
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Fact]
    public void SerializeAsV1_WithArazzoType_ShouldWriteCorrectJson()
    {
        // Arrange
        var sourceDescription = new ArazzoSourceDescription
        {
            Name = "Test Arazzo Source",
            Url = new Uri("https://example.com/arazzo"),
            Type = ArazzoDescriptionType.Arazzo
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
"""
{
    "name": "Test Arazzo Source",
    "url": "https://example.com/arazzo",
    "type": "arazzo"
}
""";

        // Act
        sourceDescription.SerializeAsV1(writer);
        var jsonResult = textWriter.ToString();
        var jsonResultObject = JsonNode.Parse(jsonResult);
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var json = """
        {
            "name": "Test Source",
            "url": "https://example.com/api",
            "type": "openapi"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        // Act
        var sourceDescription = ArazzoV1Deserializer.LoadSourceDescription(jsonNode, parsingContext);

        // Assert
        Assert.Equal("Test Source", sourceDescription.Name);
        Assert.Equal("https://example.com/api", sourceDescription.Url?.ToString());
        Assert.Equal(ArazzoDescriptionType.OpenAPI, sourceDescription.Type);
    }

    [Fact]
    public void Deserialize_WithArazzoType_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var json = """
        {
            "name": "Test Arazzo Source",
            "url": "https://example.com/arazzo",
            "type": "arazzo"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        // Act
        var sourceDescription = ArazzoV1Deserializer.LoadSourceDescription(jsonNode, parsingContext);

        // Assert
        Assert.Equal("Test Arazzo Source", sourceDescription.Name);
        Assert.Equal("https://example.com/arazzo", sourceDescription.Url?.ToString());
        Assert.Equal(ArazzoDescriptionType.Arazzo, sourceDescription.Type);
    }
}