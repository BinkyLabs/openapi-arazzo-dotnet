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

        var parameter = ArazzoV1Deserializer.LoadParameter(jsonNode, parsingContext);

        Assert.Equal("limit", parameter.Name);
        Assert.Equal(ParameterLocation.Query, parameter.In);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"10\""), parameter.Value), "Parameter value does not match expected value.");
        Assert.NotNull(parameter.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(parameter.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }
}