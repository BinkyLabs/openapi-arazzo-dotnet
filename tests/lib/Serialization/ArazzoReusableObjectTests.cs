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
            Reference = "$components.parameters.userId",
            Value = "42"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "reference": "$components.parameters.userId",
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
            Reference = "$components.successActions.notify"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "reference": "$components.successActions.notify"
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
            Reference = "$components.parameters.userId"
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

    [Theory]
    [InlineData("$steps.getUser.outputs.userId")]
    [InlineData("$components.inputs.user")]
    [InlineData("external.json#$components.parameters.userId")]
    public void SerializeAsV1_ThrowsWhenReferenceDoesNotTargetReusableComponent(string reference)
    {
        var reusableObject = new ArazzoReusableObject
        {
            Reference = reference
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => reusableObject.SerializeAsV1(writer));

        Assert.Contains("must be a valid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_ShouldSetProperties()
    {
        var json = """
        {
            "reference": "$components.parameters.userId",
            "value": "99"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$components.parameters.userId", reusableObject.Reference);
        Assert.Equal("99", reusableObject.Value);
        Assert.Empty(parsingContext.Diagnostic.Errors);
    }

    [Fact]
    public void Deserialize_ShouldSetRequiredPropertiesOnly()
    {
        var json = """
        {
            "reference": "$components.failureActions.retry"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$components.failureActions.retry", reusableObject.Reference);
        Assert.Null(reusableObject.Value);
        Assert.Empty(parsingContext.Diagnostic.Errors);
    }

    [Theory]
    [InlineData("$steps.getUser.outputs.userId")]
    [InlineData("$components.inputs.user")]
    [InlineData(null)]
    public void Deserialize_WithInvalidReference_AddsDiagnosticError(string? reference)
    {
        var json = reference is null
            ? "{}"
            : $$"""
              {
                  "reference": "{{reference}}"
              }
              """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal(reference, reusableObject.Reference);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("must be a valid runtime expression", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_ShouldRejectExtensions()
    {
        var json = """
        {
            "reference": "$components.parameters.userId",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var reusableObject = ArazzoV1Deserializer.LoadReusableObject(jsonNode, parsingContext);

        Assert.Equal("$components.parameters.userId", reusableObject.Reference);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("x-flag is not a valid property"));
    }
}