using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoRequestBodyTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var requestBody = new ArazzoRequestBody
        {
            ContentType = "application/json",
            Payload = JsonNode.Parse("{\"id\":42,\"name\":\"Alice\"}"),
            Replacements = new List<ArazzoPayloadReplacement>
            {
                new ArazzoPayloadReplacement { Target = "/name", Value = JsonNode.Parse("\"Bob\"") },
                new ArazzoPayloadReplacement { Target = "/id", Value = JsonNode.Parse("\"43\"") }
            },
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
            "contentType": "application/json",
            "payload": {
                "id": 42,
                "name": "Alice"
            },
            "replacements": [
                {
                    "target": "/name",
                    "value": "Bob"
                },
                {
                    "target": "/id",
                    "value": "43"
                }
            ],
            "x-extra": {
                "note": "yes"
            }
        }
        """;

        requestBody.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "contentType": "application/json",
            "payload": { "count": 10 },
            "replacements": [
                { "target": "/count", "value": "11" }
            ],
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var requestBody = ArazzoV1Deserializer.LoadRequestBody(jsonNode, parsingContext);

        Assert.Equal("application/json", requestBody.ContentType);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"count\":10}"), requestBody.Payload), "Payload does not match expected value.");
        Assert.NotNull(requestBody.Replacements);
        Assert.Single(requestBody.Replacements!);
        Assert.Equal("/count", requestBody.Replacements![0].Target);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"11\""), requestBody.Replacements![0].Value), "Replacement value does not match expected value.");
        Assert.NotNull(requestBody.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(requestBody.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Fact]
    public void SerializeAsV1_WithContentTypeOnly_ShouldWriteContentType()
    {
        var requestBody = new ArazzoRequestBody
        {
            ContentType = "application/json"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        requestBody.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!.AsObject();

        Assert.Equal("application/json", json["contentType"]!.GetValue<string>());
        Assert.False(json.ContainsKey("payload"));
        Assert.False(json.ContainsKey("replacements"));
    }

    [Fact]
    public void SerializeAsV1_WithPayloadOnly_ShouldWritePayload()
    {
        var requestBody = new ArazzoRequestBody
        {
            Payload = JsonNode.Parse("{\"count\":10}")
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        requestBody.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!.AsObject();

        Assert.False(json.ContainsKey("contentType"));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"count\":10}"), json["payload"]));
        Assert.False(json.ContainsKey("replacements"));
    }

    [Fact]
    public void SerializeAsV1_WithReplacementsOnly_ShouldWriteReplacements()
    {
        var requestBody = new ArazzoRequestBody
        {
            Replacements = new List<ArazzoPayloadReplacement>
            {
                new ArazzoPayloadReplacement { Target = "/count", Value = JsonValue.Create(11)! }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        requestBody.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!.AsObject();

        Assert.False(json.ContainsKey("contentType"));
        Assert.False(json.ContainsKey("payload"));
        Assert.Single(json["replacements"]!.AsArray());
    }

    [Fact]
    public void SerializeAsV1_WithNoProperties_ShouldWriteEmptyObject()
    {
        var requestBody = new ArazzoRequestBody();
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        requestBody.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!.AsObject();

        Assert.Empty(json);
    }

    [Fact]
    public void SerializeAsV1_WithInvalidEmbeddedRuntimeExpressionInPayload_ThrowsArazzoSerializationException()
    {
        var requestBody = new ArazzoRequestBody
        {
            Payload = JsonNode.Parse("""{ "id": "{$response.statusCode}" }""")
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => requestBody.SerializeAsV1(writer));

        Assert.Contains("ArazzoRequestBody.Payload contains an invalid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_WithInvalidEmbeddedRuntimeExpressionInPayload_AddsDiagnosticError()
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "payload": {
                    "id": "{$response.statusCode}"
                }
            }
            """)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadRequestBody(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoRequestBody.Payload contains an invalid runtime expression", StringComparison.Ordinal));
    }
}