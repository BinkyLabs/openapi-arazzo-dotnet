using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoCriterionTests
{
    [Fact]
    public void SerializeAsV1_WithSimpleType_ShouldWriteTypeAsString()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "response",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Simple,
                Version = null
            },
            Condition = "$.status == 200",
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonNode.Parse("{\"note\":\"success\"}")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "context": "response",
            "type": "simple",
            "condition": "$.status == 200",
            "x-extra": {
                "note": "success"
            }
        }
        """;

        criterion.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithRegexType_ShouldWriteTypeAsString()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "response",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Regex,
                Version = null
            },
            Condition = "/^[0-9]{3}$/"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "context": "response",
            "type": "regex",
            "condition": "/^[0-9]{3}$/"
        }
        """;

        criterion.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithJsonPathType_ShouldWriteTypeAsObject()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "response",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.JsonPath,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            },
            Condition = "$.status"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "context": "response",
            "type": {
                "type": "jsonpath",
                "version": "draft-goessner-dispatch-jsonpath-00"
            },
            "condition": "$.status"
        }
        """;

        criterion.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithXPathType_ShouldWriteTypeAsObject()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "response",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.XPath,
                Version = ArazzoCriterionExpressionVersion.XPath30
            },
            Condition = "/response/status"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "context": "response",
            "type": {
                "type": "xpath",
                "version": "xpath-30"
            },
            "condition": "/response/status"
        }
        """;

        criterion.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithoutType_ShouldNotWriteTypeProperty()
    {
        var criterion = new ArazzoCriterion
        {
            Condition = "$.status == 200"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "condition": "$.status == 200"
        }
        """;

        criterion.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithSimpleTypeAndVersion_ShouldThrowArazzoException()
    {
        var criterion = new ArazzoCriterion
        {
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Simple,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            },
            Condition = "test"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var ex = Assert.Throws<ArazzoException>(() => criterion.SerializeAsV1(writer));
        Assert.Contains("cannot have a version property", ex.Message);
    }

    [Fact]
    public void SerializeAsV1_WithRegexTypeAndVersion_ShouldThrowArazzoException()
    {
        var criterion = new ArazzoCriterion
        {
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Regex,
                Version = ArazzoCriterionExpressionVersion.XPath30
            },
            Condition = "/pattern/"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var ex = Assert.Throws<ArazzoException>(() => criterion.SerializeAsV1(writer));
        Assert.Contains("cannot have a version property", ex.Message);
    }

    [Fact]
    public void Deserialize_StringTypeAsSimple_ShouldCreateCriterionWithSimpleType()
    {
        var json = """
        {
            "context": "response",
            "type": "simple",
            "condition": "$.status == 200",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        var criterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        Assert.Equal("response", criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.Simple, criterion.Type.Type);
        Assert.Null(criterion.Type.Version);
        Assert.Equal("$.status == 200", criterion.Condition);
        Assert.NotNull(criterion.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(criterion.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Fact]
    public void Deserialize_StringTypeAsRegex_ShouldCreateCriterionWithRegexType()
    {
        var json = """
        {
            "type": "regex",
            "condition": "/^[0-9]+$/"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        var criterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        Assert.Null(criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.Regex, criterion.Type.Type);
        Assert.Null(criterion.Type.Version);
        Assert.Equal("/^[0-9]+$/", criterion.Condition);
    }

    [Fact]
    public void Deserialize_ObjectTypeAsJsonPath_ShouldCreateCriterionWithJsonPathType()
    {
        var json = """
        {
            "context": "response",
            "type": {
                "type": "jsonpath",
                "version": "draft-goessner-dispatch-jsonpath-00"
            },
            "condition": "$.status"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        var criterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        Assert.Equal("response", criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.JsonPath, criterion.Type.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00, criterion.Type.Version);
        Assert.Equal("$.status", criterion.Condition);
    }

    [Fact]
    public void Deserialize_ObjectTypeAsXPath_ShouldCreateCriterionWithXPathType()
    {
        var json = """
        {
            "type": {
                "type": "xpath",
                "version": "xpath-20"
            },
            "condition": "/response/status/text()"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        var criterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        Assert.Null(criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.XPath, criterion.Type.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath20, criterion.Type.Version);
        Assert.Equal("/response/status/text()", criterion.Condition);
    }

    [Fact]
    public void Deserialize_WithoutType_ShouldCreateCriterionWithNullType()
    {
        var json = """
        {
            "condition": "truthy"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        var criterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        Assert.Null(criterion.Context);
        Assert.Null(criterion.Type);
        Assert.Equal("truthy", criterion.Condition);
    }

    [Fact]
    public void RoundTrip_SimpleTypeCriterion_ShouldPreserveData()
    {
        // Serialize
        var originalCriterion = new ArazzoCriterion
        {
            Context = "response",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Simple,
                Version = null
            },
            Condition = "match_pattern"
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        originalCriterion.SerializeAsV1(writer);

        // Deserialize
        var jsonNode = JsonNode.Parse(textWriter.ToString())!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);
        var deserializedCriterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        // Assert
        Assert.Equal(originalCriterion.Context, deserializedCriterion.Context);
        Assert.NotNull(deserializedCriterion.Type);
        Assert.Equal(originalCriterion.Type.Type, deserializedCriterion.Type.Type);
        Assert.Equal(originalCriterion.Condition, deserializedCriterion.Condition);
    }

    [Fact]
    public void RoundTrip_JsonPathTypeCriterion_ShouldPreserveData()
    {
        // Serialize
        var originalCriterion = new ArazzoCriterion
        {
            Context = "response",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.JsonPath,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            },
            Condition = "$.status == 200"
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        originalCriterion.SerializeAsV1(writer);

        // Deserialize
        var jsonNode = JsonNode.Parse(textWriter.ToString())!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);
        var deserializedCriterion = ArazzoV1Deserializer.LoadCriterion(parseNode);

        // Assert
        Assert.Equal(originalCriterion.Context, deserializedCriterion.Context);
        Assert.NotNull(deserializedCriterion.Type);
        Assert.Equal(originalCriterion.Type.Type, deserializedCriterion.Type.Type);
        Assert.Equal(originalCriterion.Type.Version, deserializedCriterion.Type.Version);
        Assert.Equal(originalCriterion.Condition, deserializedCriterion.Condition);
    }
}
