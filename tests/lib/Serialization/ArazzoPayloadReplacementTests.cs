using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

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
    public void SerializeAsV1_1_ShouldWriteCorrectJson()
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

        replacement.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_ShouldSetPropertiesAndExtensions(ArazzoSpecVersion specVersion)
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

        var replacement = parsingContext.ParseFragment<ArazzoPayloadReplacement>(jsonNode, specVersion);

        Assert.NotNull(replacement);
        Assert.Equal("/data/count", replacement.Target);
        Assert.NotNull(replacement.Value);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"10\""), replacement.Value.Any), "Replacement value does not match expected value.");
        Assert.NotNull(replacement.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(replacement.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Serialize_WithNonMatchingExpressionString_WritesLiteralValue(ArazzoSpecVersion specVersion)
    {
        var replacement = new ArazzoPayloadReplacement
        {
            Target = "/data/id",
            Value = JsonNode.Parse("\"$response.statusCode\"")
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        if (specVersion is ArazzoSpecVersion.Arazzo1_0)
        {
            replacement.SerializeAsV1(writer);
        }
        else
        {
            replacement.SerializeAsV1_1(writer);
        }

        var result = JsonNode.Parse(textWriter.ToString());
        Assert.NotNull(result);
        Assert.Equal("$response.statusCode", result["value"]?.GetValue<string>());
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithNonMatchingExpressionString_SetsAny(ArazzoSpecVersion specVersion)
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "target": "/data/id",
                "value": "$response.statusCode"
            }
            """)!;
        var parsingContext = new ParsingContext(new());

        var replacement = parsingContext.ParseFragment<ArazzoPayloadReplacement>(jsonNode, specVersion);

        Assert.NotNull(replacement);
        Assert.NotNull(replacement.Value);
        Assert.NotNull(replacement.Value.Any);
        Assert.Equal("$response.statusCode", replacement.Value.Any.GetValue<string>());
        Assert.Null(replacement.Value.Expression);
        Assert.Null(replacement.Value.Selector);
        Assert.Empty(parsingContext.Diagnostic.Errors);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{ "value": "updated" }""", "ArazzoPayloadReplacement.Target is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{ "target": "", "value": "updated" }""", "ArazzoPayloadReplacement.Target is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{ "target": "/name" }""", "ArazzoPayloadReplacement.Value is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{ "value": "updated" }""", "ArazzoPayloadReplacement.Target is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{ "target": "", "value": "updated" }""", "ArazzoPayloadReplacement.Target is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{ "target": "/name" }""", "ArazzoPayloadReplacement.Value is a REQUIRED field")]
    public void ParseFragment_AsV1AndV1_1_MissingRequiredFields_AddsDiagnosticError(ArazzoSpecVersion specVersion, string json, string expectedMessage)
    {
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        parsingContext.ParseFragment<ArazzoPayloadReplacement>(jsonNode, specVersion);

        Assert.Single(parsingContext.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }
}