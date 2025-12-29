using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoPayloadReplacementTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var replacement = new ArazzoPayloadReplacement
        {
            Target = "/data/id",
            Value = JsonNode.Parse("\"42\""),
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonNode.Parse("{\"note\":\"yes\"}")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "target": "/data/id",
            "value": "42",
            "x-extra": {
                "note": "yes"
            }
        }
        """;

        replacement.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "target": "/data/count",
            "value": "10",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        var replacement = ArazzoV1Deserializer.LoadPayloadReplacement(parseNode);

        Assert.Equal("/data/count", replacement.Target);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"10\""), replacement.Value), "Replacement value does not match expected value.");
        Assert.NotNull(replacement.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(replacement.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }
}