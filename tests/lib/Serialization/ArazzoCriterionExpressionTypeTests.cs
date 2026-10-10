using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoCriterionExpressionTypeTests
{
    [Fact]
    public void SerializeAsV1_WithJsonPathType_ShouldWriteCorrectJson()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.JsonPath,
            Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00,
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
            "type": "jsonpath",
            "version": "draft-goessner-dispatch-jsonpath-00",
            "x-extra": {
                "note": "yes"
            }
        }
        """;

        expressionType.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_1_WithJsonPathType_ShouldWriteCorrectJson()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.JsonPath,
            Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00,
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
            "type": "jsonpath",
            "version": "draft-goessner-dispatch-jsonpath-00",
            "x-extra": {
                "note": "yes"
            }
        }
        """;

        expressionType.SerializeAsV1_1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithXPathType_ShouldWriteCorrectJson()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.XPath,
            Version = ArazzoCriterionExpressionVersion.XPath30
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "type": "xpath",
            "version": "xpath-30"
        }
        """;

        expressionType.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }


    [Fact]
    public void SerializeAsV1_1_WithXPathType_ShouldWriteCorrectJson()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.XPath,
            Version = ArazzoCriterionExpressionVersion.XPath30
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "type": "xpath",
            "version": "xpath-30"
        }
        """;

        expressionType.SerializeAsV1_1(writer);
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
            "type": "xpath",
            "version": "xpath-20",
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var expressionType = parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, specVersion);

        Assert.NotNull(expressionType);
        Assert.Equal(ArazzoCriterionExpressionTypeType.XPath, expressionType.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath20, expressionType.Version);
        Assert.NotNull(expressionType.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(expressionType.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithJsonPath_ShouldSetPropertiesCorrectly(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "type": "jsonpath",
            "version": "draft-goessner-dispatch-jsonpath-00"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var expressionType = parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, specVersion);

        Assert.NotNull(expressionType);
        Assert.Equal(ArazzoCriterionExpressionTypeType.JsonPath, expressionType.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00, expressionType.Version);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithXPath10_ShouldSetPropertiesCorrectly(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "type": "xpath",
            "version": "xpath-10"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var expressionType = parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, specVersion);

        Assert.NotNull(expressionType);
        Assert.Equal(ArazzoCriterionExpressionTypeType.XPath, expressionType.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath10, expressionType.Version);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{ "version": "xpath-30" }""", "ArazzoCriterionExpressionType.Type is a REQUIRED field.")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{ "type": "xpath" }""", "ArazzoCriterionExpressionType.Version is a REQUIRED field.")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{ "version": "xpath-30" }""", "ArazzoCriterionExpressionType.Type is a REQUIRED field.")]
    public void ParseFragment_AsV1AndV1_1_MissingRequiredFields_AddsDiagnosticError(ArazzoSpecVersion specVersion, string json, string expectedMessage)
    {
        var jsonNode = JsonNode.Parse(json)!;
        var diagnostic = new ArazzoDiagnostic();
        var parsingContext = new ParsingContext(diagnostic);

        parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, specVersion);

        Assert.Contains(diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("jsonpath", ArazzoCriterionExpressionVersion.Rfc9535)]
    [InlineData("xpath", ArazzoCriterionExpressionVersion.XPath31)]
    [InlineData("jsonpointer", ArazzoCriterionExpressionVersion.Rfc6901)]
    public void ParseFragment_AsV1_1_WithoutVersion_AppliesDefaultVersion(string type, ArazzoCriterionExpressionVersion expectedVersion)
    {
        var jsonNode = JsonNode.Parse($$"""{ "type": "{{type}}" }""")!;
        var diagnostic = new ArazzoDiagnostic();
        var parsingContext = new ParsingContext(diagnostic);

        var expressionType = parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(expressionType);
        Assert.Equal(expectedVersion, expressionType.Version);
        Assert.Empty(diagnostic.Errors);
    }

    [Fact]
    public void SerializeAsV1_WithoutTypeThrowsException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Version = ArazzoCriterionExpressionVersion.XPath30
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => expressionType.SerializeAsV1(writer));
    }


    [Fact]
    public void SerializeAsV1_1_WithoutTypeThrowsException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Version = ArazzoCriterionExpressionVersion.XPath30
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => expressionType.SerializeAsV1_1(writer));
    }

    [Fact]
    public void SerializeAsV1_WithoutVersionThrowsException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.JsonPath
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => expressionType.SerializeAsV1(writer));
    }


    [Fact]
    public void SerializeAsV1_1_WithoutVersionWritesDefaultVersion()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.JsonPath
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        expressionType.SerializeAsV1_1(writer);

        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        Assert.Equal("rfc9535", jsonResultObject?["version"]?.GetValue<string>());
    }

    [Fact]
    public void SerializeAsV1_WithSimpleType_ThrowsArazzoException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.Simple,
            Version = ArazzoCriterionExpressionVersion.XPath30
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoException>(() => expressionType.SerializeAsV1(writer));
        Assert.Contains("Serializing criterion expression type 'simple' as an object is NOT supported by the specification", exception.Message);
    }


    [Fact]
    public void SerializeAsV1_1_WithSimpleType_ThrowsArazzoException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.Simple,
            Version = ArazzoCriterionExpressionVersion.XPath30
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoException>(() => expressionType.SerializeAsV1_1(writer));
        Assert.Contains("Serializing criterion expression type 'simple' as an object is NOT supported by the specification", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithRegexType_ThrowsArazzoException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.Regex,
            Version = ArazzoCriterionExpressionVersion.XPath20
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoException>(() => expressionType.SerializeAsV1(writer));
        Assert.Contains("Serializing criterion expression type 'regex' as an object is NOT supported by the specification", exception.Message);
    }


    [Fact]
    public void SerializeAsV1_1_WithRegexType_ThrowsArazzoException()
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.Regex,
            Version = ArazzoCriterionExpressionVersion.XPath20
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoException>(() => expressionType.SerializeAsV1_1(writer));
        Assert.Contains("Serializing criterion expression type 'regex' as an object is NOT supported by the specification", exception.Message);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithSimpleType_ShouldLogError(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "type": "simple",
            "version": "xpath-10"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var diagnostic = new ArazzoDiagnostic();
        var parsingContext = new ParsingContext(diagnostic);

        var expressionType = parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, specVersion);

        // Verify that the object was deserialized
        Assert.Equal(ArazzoCriterionExpressionTypeType.Simple, expressionType?.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath10, expressionType?.Version);

        // Verify that an error was logged to diagnostics
        Assert.Single(diagnostic.Errors);
        Assert.Contains("Deserializing criterion expression type 'simple' as an object is NOT supported by the specification", diagnostic.Errors[0].Message);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_AsV1AndV1_1_WithRegexType_ShouldLogError(ArazzoSpecVersion specVersion)
    {
        var json = """
        {
            "type": "regex",
            "version": "xpath-30"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var diagnostic = new ArazzoDiagnostic();
        var parsingContext = new ParsingContext(diagnostic);

        var expressionType = parsingContext.ParseFragment<ArazzoCriterionExpressionType>(jsonNode, specVersion);

        // Verify that the object was deserialized
        Assert.Equal(ArazzoCriterionExpressionTypeType.Regex, expressionType?.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath30, expressionType?.Version);

        // Verify that an error was logged to diagnostics
        Assert.Single(diagnostic.Errors);
        Assert.Contains("Deserializing criterion expression type 'regex' as an object is NOT supported by the specification", diagnostic.Errors[0].Message);
    }
}