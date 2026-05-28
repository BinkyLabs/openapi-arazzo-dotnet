using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoReusableObjectTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var reusableObject = new ArazzoReusableObject
        {
            Reference = "$steps.getUser.outputs.userId",
            Value = "42",
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
            "reference": "$steps.getUser.outputs.userId",
            "value": "42",
            "x-extra": {
                "note": "yes"
            }
        }
        """;

        reusableObject.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_ShouldWriteRequiredPropertiesOnly()
    {
        var reusableObject = new ArazzoReusableObject
        {
            Reference = "$steps.getUser.outputs.userId"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "reference": "$steps.getUser.outputs.userId"
        }
        """;

        reusableObject.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_ThrowsOnNullWriter()
    {
        var reusableObject = new ArazzoReusableObject
        {
            Reference = "$steps.getUser.outputs.userId"
        };

        Assert.Throws<ArgumentNullException>(() => reusableObject.SerializeAsV1(null!));
    }

    [Fact]
    public void SerializeAsV1_ThrowsWhenReferenceIsNull()
    {
        var reusableObject = new ArazzoReusableObject();
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => reusableObject.SerializeAsV1(writer));
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "reference": "$steps.getUser.outputs.userId",
            "value": "99",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$steps.getUser.outputs.userId", reusableObject.Reference);
        Assert.Equal("99", reusableObject.Value);
        Assert.NotNull(reusableObject.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(reusableObject.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Fact]
    public void Deserialize_ShouldSetRequiredPropertiesOnly()
    {
        var json = """
        {
            "reference": "$steps.getUser.outputs.userId"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$steps.getUser.outputs.userId", reusableObject.Reference);
        Assert.Null(reusableObject.Value);
        Assert.Null(reusableObject.Extensions);
    }
}