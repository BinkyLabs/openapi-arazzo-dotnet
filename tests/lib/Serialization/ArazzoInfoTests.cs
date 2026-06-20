using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoInfoTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        // Arrange
        var arazzoInfo = new ArazzoInfo
        {
            Title = "Test Arazzo",
            Version = "1.0.0",
            Summary = "A concise summary",
            Description = "A longer description"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
"""
{
    "title": "Test Arazzo",
    "version": "1.0.0",
    "summary": "A concise summary",
    "description": "A longer description"
}
""";

        // Act
        arazzoInfo.SerializeAsV1(writer);
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
            "title": "Test Arazzo",
            "version": "1.0.0",
            "summary": "A concise summary",
            "description": "A longer description"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());


        // Act
        var arazzoInfo = ArazzoV1Deserializer.LoadInfo(jsonNode, parsingContext);

        // Assert
        Assert.Equal("Test Arazzo", arazzoInfo.Title);
        Assert.Equal("1.0.0", arazzoInfo.Version);
        Assert.Equal("A concise summary", arazzoInfo.Summary);
        Assert.Equal("A longer description", arazzoInfo.Description);
    }

    [Fact]
    public void SerializeAsV1_WithMissingTitle_ShouldThrowArgumentNullException()
    {
        var arazzoInfo = new ArazzoInfo
        {
            Version = "1.0.0"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => arazzoInfo.SerializeAsV1(writer));
    }

    [Fact]
    public void SerializeAsV1_WithMissingVersion_ShouldThrowArgumentNullException()
    {
        var arazzoInfo = new ArazzoInfo
        {
            Title = "Test Arazzo"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => arazzoInfo.SerializeAsV1(writer));
    }
}