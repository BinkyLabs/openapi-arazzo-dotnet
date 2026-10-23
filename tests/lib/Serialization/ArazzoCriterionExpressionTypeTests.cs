using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoCriterionExpressionTypeTests
{
    public static TheoryData<ArazzoSpecVersion, string, string, bool> TypeVersionPairs
    {
        get
        {
            var versionsByType = new Dictionary<string, string[]>
            {
                ["jsonpath"] = ["draft-goessner-dispatch-jsonpath-00", "rfc9535"],
                ["xpath"] = ["xpath-10", "xpath-20", "xpath-30", "xpath-31"],
                ["jsonpointer"] = ["rfc6901"]
            };
            string[] newVersions = ["rfc9535", "xpath-31", "rfc6901"];
            var data = new TheoryData<ArazzoSpecVersion, string, string, bool>();
            foreach (var specVersion in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
            {
                foreach (var (type, supportedVersions) in versionsByType)
                {
                    foreach (var version in versionsByType.Values.SelectMany(versions => versions))
                    {
                        var supported = supportedVersions.Contains(version, StringComparer.Ordinal)
                            && (specVersion is ArazzoSpecVersion.Arazzo1_1 || !newVersions.Contains(version, StringComparer.Ordinal));
                        data.Add(specVersion, type, version, supported);
                    }
                }
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(TypeVersionPairs))]
    public void TypeVersionPairs_AreEnforcedByReaderAndSerializer(ArazzoSpecVersion specVersion, string type, string version, bool supported)
    {
        var json = JsonNode.Parse($$"""{"type":"{{type}}","version":"{{version}}"}""");
        Assert.NotNull(json);
        var context = new ParsingContext(new());

        var expressionType = context.ParseFragment<ArazzoCriterionExpressionType>(json, specVersion);

        Assert.NotNull(expressionType);
        Assert.Equal(type, expressionType.Type?.GetDisplayName());
        Assert.Equal(version, expressionType.Version?.GetDisplayName());
        if (supported)
        {
            Assert.Empty(context.Diagnostic.Errors);
            Assert.True(JsonNode.DeepEquals(json, JsonNode.Parse(Serialize(expressionType, specVersion))));
        }
        else
        {
            Assert.NotEmpty(context.Diagnostic.Errors);
            Assert.Throws<ArazzoSerializationException>(() => Serialize(expressionType, specVersion));
        }
    }

    [Theory]
    [InlineData("type", "\"unknown\"")]
    [InlineData("type", "42")]
    [InlineData("type", "null")]
    [InlineData("version", "\"unknown\"")]
    [InlineData("version", "\"\"")]
    [InlineData("version", "42")]
    [InlineData("version", "null")]
    public void Deserialize_InvalidEnumFields_ReportErrorsWithoutDefaulting(string property, string value)
    {
        foreach (var specVersion in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
        {
            var json = JsonNode.Parse("""{"type":"jsonpath","version":"draft-goessner-dispatch-jsonpath-00"}""");
            Assert.NotNull(json);
            json[property] = JsonNode.Parse(value);
            var context = new ParsingContext(new());

            var expressionType = context.ParseFragment<ArazzoCriterionExpressionType>(json, specVersion);

            Assert.NotNull(expressionType);
            Assert.NotEmpty(context.Diagnostic.Errors);
            if (property == "type")
            {
                Assert.Null(expressionType.Type);
            }
            else
            {
                Assert.Null(expressionType.Version);
            }
        }
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, true)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, true)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, false)]
    public void Serialize_UndefinedEnumValues_Throw(ArazzoSpecVersion specVersion, bool invalidType)
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = invalidType ? (ArazzoCriterionExpressionTypeType)999 : ArazzoCriterionExpressionTypeType.JsonPath,
            Version = invalidType ? null : (ArazzoCriterionExpressionVersion)999
        };

        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(expressionType, specVersion));

        Assert.Contains(invalidType ? "Invalid expression type" : "not valid for type", exception.Message, StringComparison.Ordinal);
    }

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


    [Theory]
    [InlineData(ArazzoCriterionExpressionTypeType.JsonPath, "rfc9535")]
    [InlineData(ArazzoCriterionExpressionTypeType.XPath, "xpath-31")]
    [InlineData(ArazzoCriterionExpressionTypeType.JsonPointer, "rfc6901")]
    public void SerializeAsV1_1_WithoutVersionWritesDefaultVersion(ArazzoCriterionExpressionTypeType type, string expectedVersion)
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = type
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        expressionType.SerializeAsV1_1(writer);

        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        Assert.Equal(expectedVersion, jsonResultObject?["version"]?.GetValue<string>());
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

    private static string Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        if (version is ArazzoSpecVersion.Arazzo1_0)
        {
            value.SerializeAsV1(writer);
        }
        else
        {
            value.SerializeAsV1_1(writer);
        }
        return text.ToString();
    }
}