using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoComponentTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var component = new ArazzoComponent
        {
            Parameters = new Dictionary<string, ArazzoParameter>
            {
                ["param1"] = new ArazzoParameter
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Value = "123"
                }
            },
            SuccessActions = new Dictionary<string, ArazzoSuccessAction>
            {
                ["success1"] = new ArazzoSuccessAction { Name = "success1", Type = ArazzoSuccessType.End }
            },
            FailureActions = new Dictionary<string, ArazzoFailureAction>
            {
                ["failure1"] = new ArazzoFailureAction { Name = "failure1", Type = ArazzoFailureType.End }
            },
            Inputs = new Dictionary<string, IArazzoInput>
            {
                ["input1"] = new ArazzoInput { Type = JsonSchemaType.String }
            },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-custom"] = new JsonNodeExtension(JsonNode.Parse("\"test\"")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "parameters": {
                "param1": {
                    "name": "id",
                    "in": "path",
                    "value": "123"
                }
            },
            "successActions": {
                "success1": {
                    "name": "success1",
                    "type": "end"
                }
            },
            "failureActions": {
                "failure1": {
                    "name": "failure1",
                    "type": "end"
                }
            },
            "inputs": {
                "input1": {
                    "type": "string"
                }
            },
            "x-custom": "test"
        }
        """;

        component.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_1_ShouldWriteCorrectJson()
    {
        var component = new ArazzoComponent
        {
            Parameters = new Dictionary<string, ArazzoParameter>
            {
                ["param1"] = new ArazzoParameter
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Value = "123"
                }
            },
            SuccessActions = new Dictionary<string, ArazzoSuccessAction>
            {
                ["success1"] = new ArazzoSuccessAction { Name = "success1", Type = ArazzoSuccessType.End }
            },
            FailureActions = new Dictionary<string, ArazzoFailureAction>
            {
                ["failure1"] = new ArazzoFailureAction { Name = "failure1", Type = ArazzoFailureType.End }
            },
            Inputs = new Dictionary<string, IArazzoInput>
            {
                ["input1"] = new ArazzoInput { Type = JsonSchemaType.String }
            },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-custom"] = new JsonNodeExtension(JsonNode.Parse("\"test\"")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "parameters": {
                "param1": {
                    "name": "id",
                    "in": "path",
                    "value": "123"
                }
            },
            "successActions": {
                "success1": {
                    "name": "success1",
                    "type": "end"
                }
            },
            "failureActions": {
                "failure1": {
                    "name": "failure1",
                    "type": "end"
                }
            },
            "inputs": {
                "input1": {
                    "type": "string"
                }
            },
            "x-custom": "test"
        }
        """;

        component.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "parameters": {
                "param1": {
                    "name": "id",
                    "in": "path",
                    "value": "456"
                }
            },
            "successActions": {
                "success1": {
                    "name": "success1",
                    "type": "end"
                }
            },
            "inputs": {
                "input1": {
                    "type": "string"
                }
            },
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var component = ArazzoV1Deserializer.LoadComponent(jsonNode, parsingContext);

        Assert.NotNull(component.Parameters);
        Assert.Contains("param1", component.Parameters!.Keys);
        Assert.Equal("id", component.Parameters["param1"].Name);
        Assert.Equal(ParameterLocation.Path, component.Parameters["param1"].In);

        Assert.NotNull(component.SuccessActions);
        Assert.Contains("success1", component.SuccessActions!.Keys);
        Assert.Equal("success1", component.SuccessActions["success1"].Name);
        Assert.Equal(ArazzoSuccessType.End, component.SuccessActions["success1"].Type);

        Assert.NotNull(component.Inputs);
        Assert.Equal(JsonSchemaType.String, component.Inputs!["input1"].Type);

        Assert.NotNull(component.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(component.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Fact]
    public void SerializeAsV1_ShouldHandleNullCollections()
    {
        var component = new ArazzoComponent();
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson = "{ }";

        component.SerializeAsV1(writer);
        var result = textWriter.ToString();

        Assert.Equal(expectedJson, result);
    }


    [Fact]
    public void SerializeAsV1_1_ShouldHandleNullCollections()
    {
        var component = new ArazzoComponent();
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson = "{ }";

        component.SerializeAsV1_1(writer);
        var result = textWriter.ToString();

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public void SerializeAsV1_WithInvalidComponentKey_ThrowsArazzoSerializationException()
    {
        var component = new ArazzoComponent
        {
            Parameters = new Dictionary<string, ArazzoParameter>
            {
                ["invalid key"] = new()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Value = "123"
                }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => component.SerializeAsV1(writer));

        Assert.Contains("Invalid key: 'invalid key'", exception.Message);
    }


    [Fact]
    public void SerializeAsV1_1_WithInvalidComponentKey_ThrowsArazzoSerializationException()
    {
        var component = new ArazzoComponent
        {
            Parameters = new Dictionary<string, ArazzoParameter>
            {
                ["invalid key"] = new()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Value = "123"
                }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => component.SerializeAsV1_1(writer));

        Assert.Contains("Invalid key: 'invalid key'", exception.Message);
    }

    [Fact]
    public void Deserialize_WithInvalidComponentKey_AddsDiagnosticError()
    {
        var json = """
        {
            "parameters": {
                "invalid key": {
                    "name": "id",
                    "in": "path",
                    "value": "456"
                }
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var component = ArazzoV1Deserializer.LoadComponent(jsonNode, parsingContext);

        Assert.NotNull(component.Parameters);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("Invalid key: 'invalid key'", StringComparison.Ordinal));
    }
}