using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoCriterionTests
{
    [Fact]
    public void SerializeAsV1_WithSimpleType_ShouldWriteTypeAsString()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
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
    public void SerializeAsV1_1_WithSimpleType_ShouldWriteTypeAsString()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
            "type": "simple",
            "condition": "$.status == 200",
            "x-extra": {
                "note": "success"
            }
        }
        """;

        criterion.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithRegexType_ShouldWriteTypeAsString()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
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
    public void SerializeAsV1_1_WithRegexType_ShouldWriteTypeAsString()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
            "type": "regex",
            "condition": "/^[0-9]{3}$/"
        }
        """;

        criterion.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithJsonPathType_ShouldWriteTypeAsObject()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
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
    public void SerializeAsV1_1_WithJsonPathType_ShouldWriteTypeAsObject()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
            "type": {
                "type": "jsonpath",
                "version": "draft-goessner-dispatch-jsonpath-00"
            },
            "condition": "$.status"
        }
        """;

        criterion.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithXPathType_ShouldWriteTypeAsObject()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
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
    public void SerializeAsV1_1_WithXPathType_ShouldWriteTypeAsObject()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
            "context": "$response.body",
            "type": {
                "type": "xpath",
                "version": "xpath-30"
            },
            "condition": "/response/status"
        }
        """;

        criterion.SerializeAsV1_1(writer);
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
    public void SerializeAsV1_1_WithoutType_ShouldNotWriteTypeProperty()
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

        criterion.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithSimpleTypeAndVersion_ShouldThrowArazzoException()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
    public void SerializeAsV1_1_WithSimpleTypeAndVersion_ShouldThrowArazzoException()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Simple,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            },
            Condition = "test"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var ex = Assert.Throws<ArazzoException>(() => criterion.SerializeAsV1_1(writer));
        Assert.Contains("cannot have a version property", ex.Message);
    }

    [Fact]
    public void SerializeAsV1_WithRegexTypeAndVersion_ShouldThrowArazzoException()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
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
    public void SerializeAsV1_1_WithRegexTypeAndVersion_ShouldThrowArazzoException()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "$response.body",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Regex,
                Version = ArazzoCriterionExpressionVersion.XPath30
            },
            Condition = "/pattern/"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var ex = Assert.Throws<ArazzoException>(() => criterion.SerializeAsV1_1(writer));
        Assert.Contains("cannot have a version property", ex.Message);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_StringTypeAsSimple_ShouldCreateCriterionWithSimpleType(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "context": "$response.body",
            "type": "simple",
            "condition": "$.status == 200",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var criterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.NotNull(criterion);
        Assert.Equal("$response.body", criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.Simple, criterion.Type.Type);
        Assert.Null(criterion.Type.Version);
        Assert.Equal("$.status == 200", criterion.Condition);
        Assert.NotNull(criterion.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(criterion.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_StringTypeAsRegex_ShouldCreateCriterionWithRegexType(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "context": "$response.body",
            "type": "regex",
            "condition": "/^[0-9]+$/"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var criterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.NotNull(criterion);
        Assert.Equal("$response.body", criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.Regex, criterion.Type.Type);
        Assert.Null(criterion.Type.Version);
        Assert.Equal("/^[0-9]+$/", criterion.Condition);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_ObjectTypeAsJsonPath_ShouldCreateCriterionWithJsonPathType(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "context": "$response.body",
            "type": {
                "type": "jsonpath",
                "version": "draft-goessner-dispatch-jsonpath-00"
            },
            "condition": "$.status"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var criterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.NotNull(criterion);
        Assert.Equal("$response.body", criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.JsonPath, criterion.Type.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00, criterion.Type.Version);
        Assert.Equal("$.status", criterion.Condition);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_ObjectTypeAsXPath_ShouldCreateCriterionWithXPathType(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "context": "$response.body",
            "type": {
                "type": "xpath",
                "version": "xpath-20"
            },
            "condition": "/response/status/text()"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var criterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.NotNull(criterion);
        Assert.Equal("$response.body", criterion.Context);
        Assert.NotNull(criterion.Type);
        Assert.Equal(ArazzoCriterionExpressionTypeType.XPath, criterion.Type.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath20, criterion.Type.Version);
        Assert.Equal("/response/status/text()", criterion.Condition);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithoutType_ShouldCreateCriterionWithNullType(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "condition": "truthy"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var criterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.NotNull(criterion);
        Assert.Null(criterion.Context);
        Assert.Null(criterion.Type);
        Assert.Equal("truthy", criterion.Condition);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void RoundTrip_AsV1AndV1_1_SimpleTypeCriterion_ShouldPreserveData(ArazzoSpecVersion specVersion)
    {
        // Serialize
        var originalCriterion = new ArazzoCriterion
        {
            Context = "$response.body",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Simple,
                Version = null
            },
            Condition = "match_pattern"
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        if (specVersion == ArazzoSpecVersion.Arazzo1_0)
        {
            originalCriterion.SerializeAsV1(writer);
        }
        else
        {
            originalCriterion.SerializeAsV1_1(writer);
        }

        // Deserialize
        var jsonNode = JsonNode.Parse(textWriter.ToString())!;
        var parsingContext = new ParsingContext(new());
        var deserializedCriterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        // Assert
        Assert.NotNull(deserializedCriterion);
        Assert.Equal(originalCriterion.Context, deserializedCriterion.Context);
        Assert.NotNull(deserializedCriterion.Type);
        Assert.Equal(originalCriterion.Type.Type, deserializedCriterion.Type.Type);
        Assert.Equal(originalCriterion.Condition, deserializedCriterion.Condition);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void RoundTrip_AsV1AndV1_1_JsonPathTypeCriterion_ShouldPreserveData(ArazzoSpecVersion specVersion)
    {
        // Serialize
        var originalCriterion = new ArazzoCriterion
        {
            Context = "$response.body",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.JsonPath,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            },
            Condition = "$.status == 200"
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        if (specVersion == ArazzoSpecVersion.Arazzo1_0)
        {
            originalCriterion.SerializeAsV1(writer);
        }
        else
        {
            originalCriterion.SerializeAsV1_1(writer);
        }

        // Deserialize
        var jsonNode = JsonNode.Parse(textWriter.ToString())!;
        var parsingContext = new ParsingContext(new());
        var deserializedCriterion = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        // Assert
        Assert.NotNull(deserializedCriterion);
        Assert.Equal(originalCriterion.Context, deserializedCriterion.Context);
        Assert.NotNull(deserializedCriterion.Type);
        Assert.Equal(originalCriterion.Type.Type, deserializedCriterion.Type.Type);
        Assert.Equal(originalCriterion.Type.Version, deserializedCriterion.Type.Version);
        Assert.Equal(originalCriterion.Condition, deserializedCriterion.Condition);
    }

    [Fact]
    public void SerializeAsV1_WithInvalidContext_ThrowsArazzoSerializationException()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "response",
            Condition = "true"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => criterion.SerializeAsV1(writer));

        Assert.Contains("ArazzoCriterion.Context must be a valid runtime expression", exception.Message, StringComparison.Ordinal);
    }


    [Fact]
    public void SerializeAsV1_1_WithInvalidContext_ThrowsArazzoSerializationException()
    {
        var criterion = new ArazzoCriterion
        {
            Context = "response",
            Condition = "true"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => criterion.SerializeAsV1_1(writer));

        Assert.Contains("ArazzoCriterion.Context must be a valid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithoutCondition_ThrowsArazzoSerializationException()
    {
        var criterion = new ArazzoCriterion();
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => criterion.SerializeAsV1(writer));

        Assert.Contains("ArazzoCriterion.Condition is required", exception.Message, StringComparison.Ordinal);
    }


    [Fact]
    public void SerializeAsV1_1_WithoutCondition_ThrowsArazzoSerializationException()
    {
        var criterion = new ArazzoCriterion();
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => criterion.SerializeAsV1_1(writer));

        Assert.Contains("ArazzoCriterion.Condition is required", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithTypeAndNoContext_ThrowsArazzoSerializationException()
    {
        var criterion = new ArazzoCriterion
        {
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Regex
            },
            Condition = "/^[0-9]+$/"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => criterion.SerializeAsV1(writer));

        Assert.Contains("ArazzoCriterion.Context is required when ArazzoCriterion.Type is specified", exception.Message, StringComparison.Ordinal);
    }


    [Fact]
    public void SerializeAsV1_1_WithTypeAndNoContext_ThrowsArazzoSerializationException()
    {
        var criterion = new ArazzoCriterion
        {
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Regex
            },
            Condition = "/^[0-9]+$/"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => criterion.SerializeAsV1_1(writer));

        Assert.Contains("ArazzoCriterion.Context is required when ArazzoCriterion.Type is specified", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithInvalidContext_AddsDiagnosticError(ArazzoSpecVersion specVersion)
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "context": "response",
                "condition": "true"
            }
            """)!;
        var parsingContext = new ParsingContext(new());

        _ = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoCriterion.Context must be a valid runtime expression", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithoutCondition_AddsDiagnosticError(ArazzoSpecVersion specVersion)
    {
        var jsonNode = JsonNode.Parse("{}")!;
        var parsingContext = new ParsingContext(new());

        _ = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoCriterion.Condition is required", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithTypeAndNoContext_AddsDiagnosticError(ArazzoSpecVersion specVersion)
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "type": "regex",
                "condition": "/^[0-9]+$/"
            }
            """)!;
        var parsingContext = new ParsingContext(new());

        _ = parsingContext.ParseFragment<ArazzoCriterion>(jsonNode, specVersion);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("ArazzoCriterion.Context is required when ArazzoCriterion.Type is specified", StringComparison.Ordinal));
    }
}