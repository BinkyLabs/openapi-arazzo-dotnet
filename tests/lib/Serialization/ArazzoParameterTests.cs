using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoParameterTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var parameter = new ArazzoParameter
        {
            Name = "id",
            In = ParameterLocation.Path,
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
            "name": "id",
            "in": "path",
            "value": "42",
            "x-extra": {
                "note": "yes"
            }
        }
        """;

        parameter.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "name": "limit",
            "in": "query",
            "value": "10",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var parameter = Assert.IsType<ArazzoParameter>(ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext));

        Assert.Equal("limit", parameter.Name);
        Assert.Equal(ParameterLocation.Query, parameter.In);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"10\""), parameter.Value), "Parameter value does not match expected value.");
        Assert.NotNull(parameter.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(parameter.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Fact]
    public void SerializeAsV1_WithoutIn_ShouldOmitIn()
    {
        var parameter = new ArazzoParameter
        {
            Name = "input",
            Value = "42"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "name": "input",
            "value": "42"
        }
        """;

        parameter.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithReference_WritesReferenceAndValueOverride()
    {
        var parameter = new ArazzoParameterReference("shared")
        {
            Value = JsonValue.Create("42")
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        parameter.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.parameters.shared", json?["reference"]?.GetValue<string>());
        Assert.Equal("42", json?["value"]?.GetValue<string>());
    }

    [Fact]
    public void Deserialize_WithReference_ReturnsParameterReference()
    {
        var json = """
        {
            "reference": "$components.parameters.shared",
            "value": "25"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var parameter = Assert.IsType<ArazzoParameterReference>(ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext));

        Assert.Equal("$components.parameters.shared", parameter.Reference.ReferenceV1);
        Assert.Equal("25", parameter.Value?.GetValue<string>());
    }

    [Theory]
    [InlineData("$steps.getUser.outputs.userId")]
    [InlineData("$components.successActions.shared")]
    [InlineData("$components.parameters")]
    public void Deserialize_WithInvalidReusableReference_AddsDiagnosticError(string reference)
    {
        var json = $$"""
        {
            "reference": "{{reference}}"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = Assert.IsType<ArazzoParameterReference>(ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext));

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("$components.parameters.<name>", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithExternalReference_ThrowsOpenApiException()
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "reference": "external.json#$components.parameters.shared"
            }
            """)!;

        var exception = Assert.Throws<OpenApiException>(() => ArazzoV1Deserializer.LoadParameter(jsonNode, new ParsingContext(new())));

        Assert.Contains("do not support external resources", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithInvalidRuntimeExpressionValue_ThrowsArazzoSerializationException()
    {
        var parameter = new ArazzoParameter
        {
            Name = "id",
            In = ParameterLocation.Query,
            Value = "$response.statusCode"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => parameter.SerializeAsV1(writer));

        Assert.Contains("ArazzoParameter.Value contains an invalid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_WithInvalidRuntimeExpressionValue_AddsDiagnosticError()
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "name": "id",
                "in": "query",
                "value": "$response.statusCode"
            }
            """)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoParameter.Value contains an invalid runtime expression", StringComparison.Ordinal));
    }
}