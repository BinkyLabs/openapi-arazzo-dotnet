using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;
using BinkyLabs.OpenApi.Arazzo.Reader.V1_1;

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
    public void SerializeAsV1_1_ShouldWriteCorrectJson()
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

        parameter.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_ShouldSetPropertiesAndExtensions(ArazzoSpecVersion version)
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

        var parameter = LoadParameterObject(jsonNode, parsingContext, version);

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
    public void SerializeAsV1_1_WithoutIn_ShouldOmitIn()
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

        parameter.SerializeAsV1_1(writer);
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
    public void SerializeAsV1_1_WithReference_WritesReferenceAndValueOverride()
    {
        var parameter = new ArazzoParameterReference("shared")
        {
            Value = JsonValue.Create("42")
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        parameter.SerializeAsV1_1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.parameters.shared", json?["reference"]?.GetValue<string>());
        Assert.Equal("42", json?["value"]?.GetValue<string>());
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_WithReference_ReturnsParameterReference(ArazzoSpecVersion version)
    {
        var json = """
        {
            "reference": "$components.parameters.shared",
            "value": "25"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var parameter = Assert.IsType<ArazzoParameterReference>(LoadParameter(jsonNode, parsingContext, version));

        Assert.Equal("$components.parameters.shared", parameter.Reference.ReferenceV1);
        Assert.Equal("25", parameter.Value?.GetValue<string>());
        Assert.DoesNotContain(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoParameter.Name is a REQUIRED field", StringComparison.Ordinal));
        Assert.DoesNotContain(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoParameter.Value is a REQUIRED field", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_WithDollarRef_ReturnsParameterObject(ArazzoSpecVersion version)
    {
        var json = """
        {
            "$ref": "$components.parameters.shared",
            "value": "25"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var parameter = Assert.IsType<ArazzoParameter>(LoadParameter(jsonNode, parsingContext, version));

        Assert.Equal("25", parameter.Value?.GetValue<string>());
        Assert.Null(parameter.Name);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$steps.getUser.outputs.userId")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$components.successActions.shared")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$components.parameters")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$steps.getUser.outputs.userId")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$components.successActions.shared")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$components.parameters")]
    public void Deserialize_WithInvalidReusableReference_AddsDiagnosticError(ArazzoSpecVersion version, string reference)
    {
        var json = $$"""
        {
            "reference": "{{reference}}"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = Assert.IsType<ArazzoParameterReference>(LoadParameter(jsonNode, parsingContext, version));

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("$components.parameters.<name>", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_WithExternalReference_ThrowsOpenApiException(ArazzoSpecVersion version)
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "reference": "external.json#$components.parameters.shared"
            }
            """)!;

        var exception = Assert.Throws<OpenApiException>(() => LoadParameter(jsonNode, new ParsingContext(new()), version));

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
    public void SerializeAsV1_1_WithInvalidRuntimeExpressionValue_ThrowsArazzoSerializationException()
    {
        var parameter = new ArazzoParameter
        {
            Name = "id",
            In = ParameterLocation.Query,
            Value = "$response.statusCode"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => parameter.SerializeAsV1_1(writer));

        Assert.Contains("ArazzoParameter.Value contains an invalid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_WithInvalidRuntimeExpressionValue_AddsDiagnosticError(ArazzoSpecVersion version)
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

        _ = LoadParameterObject(jsonNode, parsingContext, version);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoParameter.Value contains an invalid runtime expression", StringComparison.Ordinal));
    }


    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "{ \"value\": \"42\" }", "ArazzoParameter.Name is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "{ \"name\": \"id\" }", "ArazzoParameter.Value is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "{ \"value\": \"42\" }", "ArazzoParameter.Name is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "{ \"name\": \"id\" }", "ArazzoParameter.Value is a REQUIRED field")]
    public void Deserialize_MissingRequiredFields_AddsDiagnosticError(ArazzoSpecVersion version, string json, string expectedMessage)
    {
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = LoadParameterObject(jsonNode, parsingContext, version);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    private static ArazzoParameter LoadParameterObject(JsonNode jsonNode, ParsingContext parsingContext, ArazzoSpecVersion version)
    {
        var parameter = version == ArazzoSpecVersion.Arazzo1_1
            ? ArazzoV1_1Deserializer.LoadParameterObject(jsonNode, parsingContext)
            : Assert.IsType<ArazzoParameter>(ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext));
        Assert.NotNull(parameter);
        return parameter;
    }

    private static IArazzoParameter LoadParameter(JsonNode jsonNode, ParsingContext parsingContext, ArazzoSpecVersion version)
    {
        return version == ArazzoSpecVersion.Arazzo1_1
            ? ArazzoV1_1Deserializer.LoadParameter(jsonNode, parsingContext)
            : ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext);
    }
}