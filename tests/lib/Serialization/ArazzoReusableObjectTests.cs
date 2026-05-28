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
            Value = "42"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "reference": "$steps.getUser.outputs.userId",
            "value": "42"
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
    public void Deserialize_ShouldSetProperties()
    {
        var json = """
        {
            "reference": "$steps.getUser.outputs.userId",
            "value": "99"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$steps.getUser.outputs.userId", reusableObject.Reference);
        Assert.Equal("99", reusableObject.Value);
        Assert.Empty(parsingContext.Diagnostic.Errors);
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
        Assert.Empty(parsingContext.Diagnostic.Errors);
    }

    [Fact]
    public void Deserialize_ShouldRejectExtensions()
    {
        var json = """
        {
            "reference": "$steps.getUser.outputs.userId",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$steps.getUser.outputs.userId", reusableObject.Reference);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("x-flag is not a valid property"));
    }
}