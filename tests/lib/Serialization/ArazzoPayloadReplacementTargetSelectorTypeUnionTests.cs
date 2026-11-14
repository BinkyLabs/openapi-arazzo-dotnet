using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoPayloadReplacementTargetSelectorTypeUnionTests
{
    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, ArazzoPayloadRequirementTargetSelectorType.JsonPath, "jsonpath")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, ArazzoPayloadRequirementTargetSelectorType.XPath, "xpath")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, ArazzoPayloadRequirementTargetSelectorType.JsonPointer, "jsonpointer")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, ArazzoPayloadRequirementTargetSelectorType.JsonPath, "jsonpath")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, ArazzoPayloadRequirementTargetSelectorType.XPath, "xpath")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, ArazzoPayloadRequirementTargetSelectorType.JsonPointer, "jsonpointer")]
    public void Enum_ConvertsAndRoundTripsInline(ArazzoSpecVersion version, ArazzoPayloadRequirementTargetSelectorType type, string displayName)
    {
        var replacement = new ArazzoPayloadReplacement
        {
            Target = "/id",
            TargetSelectorType = type,
            Value = JsonValue.Create(42)
        };
        var propertyName = GetPropertyName(version);
        var expected = JsonNode.Parse($$"""{"target":"/id","{{propertyName}}":"{{displayName}}","value":42}""");

        var json = JsonNode.Parse(Serialize(replacement, version));

        Assert.NotNull(json);
        Assert.True(JsonNode.DeepEquals(expected, json));
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoPayloadReplacement>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.TargetSelectorType);
        Assert.Equal(type, parsed.TargetSelectorType.Type);
        Assert.Null(parsed.TargetSelectorType.ExpressionType);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.Equal(version, context.Diagnostic.SpecificationVersion);
        Assert.True(JsonNode.DeepEquals(json, JsonNode.Parse(Serialize(parsed, version))));

        var fragmentContext = new ParsingContext(new());
        var fragment = fragmentContext.ParseFragment<ArazzoPayloadReplacementTargetSelectorTypeUnion>(JsonValue.Create(displayName), version);
        Assert.NotNull(fragment);
        Assert.Equal(type, fragment.Type);
        Assert.Null(fragment.ExpressionType);
        Assert.Empty(fragmentContext.Diagnostic.Errors);
        Assert.Equal($"\"{displayName}\"", Serialize(fragment, version));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void ExpressionType_ConvertsAndRoundTripsInlineWithExtensions(ArazzoSpecVersion version)
    {
        var expressionType = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.XPath,
            Version = ArazzoCriterionExpressionVersion.XPath20,
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-note"] = new JsonNodeExtension(JsonValue.Create("kept"))
            }
        };
        var replacement = new ArazzoPayloadReplacement
        {
            Target = "/id",
            TargetSelectorType = expressionType,
            Value = JsonValue.Create(42)
        };
        Assert.NotNull(replacement.TargetSelectorType);
        Assert.Same(expressionType, replacement.TargetSelectorType.ExpressionType);
        Assert.Null(replacement.TargetSelectorType.Type);
        var propertyName = GetPropertyName(version);
        var expected = JsonNode.Parse($$"""
            {"target":"/id","{{propertyName}}":{"type":"xpath","version":"xpath-20","x-note":"kept"},"value":42}
            """);

        var json = JsonNode.Parse(Serialize(replacement, version));

        Assert.NotNull(json);
        Assert.True(JsonNode.DeepEquals(expected, json));
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoPayloadReplacement>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.TargetSelectorType);
        Assert.Null(parsed.TargetSelectorType.Type);
        var parsedExpressionType = parsed.TargetSelectorType.ExpressionType;
        Assert.NotNull(parsedExpressionType);
        Assert.Equal(ArazzoCriterionExpressionTypeType.XPath, parsedExpressionType.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.XPath20, parsedExpressionType.Version);
        Assert.NotNull(parsedExpressionType.Extensions);
        Assert.Contains("x-note", parsedExpressionType.Extensions.Keys);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, JsonNode.Parse(Serialize(parsed, version))));

        var expressionNode = json[propertyName];
        Assert.NotNull(expressionNode);
        var fragmentContext = new ParsingContext(new());
        var fragment = fragmentContext.ParseFragment<ArazzoPayloadReplacementTargetSelectorTypeUnion>(expressionNode, version);
        Assert.NotNull(fragment);
        Assert.NotNull(fragment.ExpressionType);
        Assert.Null(fragment.Type);
        Assert.Empty(fragmentContext.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(expressionNode, JsonNode.Parse(Serialize(fragment, version))));
    }

    [Theory]
    [InlineData("jsonpath", ArazzoCriterionExpressionTypeType.JsonPath, ArazzoCriterionExpressionVersion.Rfc9535, "rfc9535")]
    [InlineData("xpath", ArazzoCriterionExpressionTypeType.XPath, ArazzoCriterionExpressionVersion.XPath31, "xpath-31")]
    [InlineData("jsonpointer", ArazzoCriterionExpressionTypeType.JsonPointer, ArazzoCriterionExpressionVersion.Rfc6901, "rfc6901")]
    public void Deserialize_V1_1ExpressionTypeAppliesDefaultVersion(string typeName, ArazzoCriterionExpressionTypeType type, ArazzoCriterionExpressionVersion expectedVersion, string versionName)
    {
        var input = JsonNode.Parse($$"""{"target":"/id","targetSelectorType":{"type":"{{typeName}}"},"value":42}""");
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var replacement = context.ParseFragment<ArazzoPayloadReplacement>(input, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(replacement);
        Assert.NotNull(replacement.TargetSelectorType);
        Assert.Null(replacement.TargetSelectorType.Type);
        var expressionType = replacement.TargetSelectorType.ExpressionType;
        Assert.NotNull(expressionType);
        Assert.Equal(type, expressionType.Type);
        Assert.Equal(expectedVersion, expressionType.Version);
        Assert.Empty(context.Diagnostic.Errors);
        var expected = JsonNode.Parse($$"""{"target":"/id","targetSelectorType":{"type":"{{typeName}}","version":"{{versionName}}"},"value":42}""");
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(Serialize(replacement, ArazzoSpecVersion.Arazzo1_1))));
    }

    public static TheoryData<ArazzoSpecVersion, string> InvalidValues
    {
        get
        {
            var data = new TheoryData<ArazzoSpecVersion, string>();
            foreach (var version in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
            {
                foreach (var json in new[] { "\"unknown\"", "\"simple\"", "\"regex\"", "\"\"", "\"0\"", "0", "true", "[]", "{}", """{"version":"xpath-20"}""" })
                {
                    data.Add(version, json);
                }
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Deserialize_RejectsInvalidEnumValuesAndShapesWithLocatedDiagnostic(ArazzoSpecVersion version, string json)
    {
        var propertyName = GetPropertyName(version);
        var input = JsonNode.Parse($$"""{"target":"/id","{{propertyName}}":{{json}},"value":42}""");
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var replacement = context.ParseFragment<ArazzoPayloadReplacement>(input, version);

        Assert.NotNull(replacement);
        Assert.Null(replacement.TargetSelectorType);
        var error = Assert.Single(context.Diagnostic.Errors);
        Assert.Contains(propertyName, error.Pointer, StringComparison.Ordinal);
        Assert.NotNull(replacement.Value);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{"type":null,"version":"xpath-20"}""", "Type is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{"type":null,"version":"xpath-20"}""", "Type is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{"type":"xpath"}""", "Version is a REQUIRED field")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{"type":"unknown","version":"xpath-20"}""", "not recognized")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, """{"type":"simple","version":"xpath-20"}""", "NOT supported")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, """{"type":"regex","version":"xpath-20"}""", "NOT supported")]
    public void Deserialize_ObjectWithTypeAlwaysUsesExpressionTypeValidation(ArazzoSpecVersion version, string json, string expectedMessage)
    {
        var input = JsonNode.Parse(json);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var result = context.ParseFragment<ArazzoPayloadReplacementTargetSelectorTypeUnion>(input, version);

        Assert.NotNull(result);
        Assert.Null(result.Type);
        Assert.NotNull(result.ExpressionType);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Deserialize_UsesExistingCaseInsensitiveEnumConvention(ArazzoSpecVersion version)
    {
        var context = new ParsingContext(new());

        var result = context.ParseFragment<ArazzoPayloadReplacementTargetSelectorTypeUnion>(JsonValue.Create("JSONPATH"), version);

        Assert.NotNull(result);
        Assert.Equal(ArazzoPayloadRequirementTargetSelectorType.JsonPath, result.Type);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.Equal("\"jsonpath\"", Serialize(result, version));
    }

    [Fact]
    public void ExpressionType_PreservesExistingVersionValidation()
    {
        ArazzoPayloadReplacementTargetSelectorTypeUnion value = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.JsonPointer,
            Version = ArazzoCriterionExpressionVersion.Rfc6901
        };
        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Contains("not supported in Arazzo 1.0", exception.Message, StringComparison.Ordinal);
        var json = JsonNode.Parse(Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        Assert.NotNull(json);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"type":"jsonpointer","version":"rfc6901"}"""), json));

        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoPayloadReplacementTargetSelectorTypeUnion>(json, ArazzoSpecVersion.Arazzo1_0);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.ExpressionType);
        Assert.Null(parsed.Type);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, context.Diagnostic.SpecificationVersion);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("not supported in Arazzo 1.0", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, true)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, true)]
    public void Serialize_RejectsEmptyAndConflictingBranches(ArazzoSpecVersion version, bool conflicting)
    {
        var value = new ArazzoPayloadReplacementTargetSelectorTypeUnion();
        if (conflicting)
        {
            value.Type = ArazzoPayloadRequirementTargetSelectorType.JsonPath;
            value.ExpressionType = new()
            {
                Type = ArazzoCriterionExpressionTypeType.XPath,
                Version = ArazzoCriterionExpressionVersion.XPath20
            };
        }

        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(value, version));
        Assert.Contains("exactly one of Type or ExpressionType", exception.Message, StringComparison.Ordinal);
        var replacement = new ArazzoPayloadReplacement { Target = "/id", TargetSelectorType = value, Value = JsonValue.Create(42) };
        Assert.Throws<ArazzoSerializationException>(() => Serialize(replacement, version));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Serialize_RejectsUndefinedEnumValue(ArazzoSpecVersion version)
    {
        ArazzoPayloadReplacementTargetSelectorTypeUnion value = (ArazzoPayloadRequirementTargetSelectorType)999;

        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(value, version));

        Assert.Contains("Invalid payload replacement target selector type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConversionAndSerialization_RejectNullArguments()
    {
        ArazzoCriterionExpressionType? expressionType = null;
        Assert.Throws<ArgumentNullException>(() => (ArazzoPayloadReplacementTargetSelectorTypeUnion)expressionType!);
        ArazzoPayloadReplacementTargetSelectorTypeUnion value = ArazzoPayloadRequirementTargetSelectorType.JsonPath;
        Assert.Throws<ArgumentNullException>(() => value.SerializeAsV1(null!));
        Assert.Throws<ArgumentNullException>(() => value.SerializeAsV1_1(null!));
    }

    [Theory]
    [InlineData(ArazzoPayloadRequirementTargetSelectorType.JsonPath)]
    [InlineData(ArazzoPayloadRequirementTargetSelectorType.XPath)]
    [InlineData(ArazzoPayloadRequirementTargetSelectorType.JsonPointer)]
    public void ImplicitEnumConversions_RoundTripSelectorLanguage(ArazzoPayloadRequirementTargetSelectorType expected)
    {
        ArazzoPayloadReplacementTargetSelectorTypeUnion wrapped = expected;

        ArazzoPayloadRequirementTargetSelectorType actual = wrapped;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ImplicitExpressionTypeConversions_PreserveInstance()
    {
        var expected = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.XPath,
            Version = ArazzoCriterionExpressionVersion.XPath20
        };
        ArazzoPayloadReplacementTargetSelectorTypeUnion wrapped = expected;

        ArazzoCriterionExpressionType actual = wrapped;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void ReverseImplicitConversions_RejectInactiveBranch()
    {
        ArazzoPayloadReplacementTargetSelectorTypeUnion enumValue = ArazzoPayloadRequirementTargetSelectorType.JsonPath;
        ArazzoPayloadReplacementTargetSelectorTypeUnion expressionValue = new ArazzoCriterionExpressionType
        {
            Type = ArazzoCriterionExpressionTypeType.XPath,
            Version = ArazzoCriterionExpressionVersion.XPath20
        };

        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionType)enumValue);
        Assert.Throws<InvalidCastException>(() => (ArazzoPayloadRequirementTargetSelectorType)expressionValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReverseImplicitConversions_RejectEmptyAndConflictingBranches(bool conflicting)
    {
        var value = new ArazzoPayloadReplacementTargetSelectorTypeUnion();
        if (conflicting)
        {
            value.Type = ArazzoPayloadRequirementTargetSelectorType.JsonPath;
            value.ExpressionType = new()
            {
                Type = ArazzoCriterionExpressionTypeType.XPath,
                Version = ArazzoCriterionExpressionVersion.XPath20
            };
        }

        Assert.Throws<InvalidCastException>(() => (ArazzoPayloadRequirementTargetSelectorType)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionType)value);
    }

    [Fact]
    public void ReverseImplicitConversions_RejectNullWrapper()
    {
        ArazzoPayloadReplacementTargetSelectorTypeUnion? value = null;

        Assert.Throws<ArgumentNullException>(() => (ArazzoPayloadRequirementTargetSelectorType)value!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoCriterionExpressionType)value!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Serialize_OmitsAbsentOptionalTargetSelectorType(ArazzoSpecVersion version)
    {
        var replacement = new ArazzoPayloadReplacement { Target = "/id", Value = JsonValue.Create(42) };

        var json = JsonNode.Parse(Serialize(replacement, version));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"target":"/id","value":42}"""), json));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void SerializeYaml_WritesEnumAndExpressionTypeWithoutWrapperNodes(ArazzoSpecVersion version)
    {
        var body = new ArazzoRequestBody
        {
            Replacements = new List<ArazzoPayloadReplacement>
            {
                new() { Target = "/a", TargetSelectorType = ArazzoPayloadRequirementTargetSelectorType.JsonPointer, Value = JsonValue.Create(42) },
                new()
                {
                    Target = "/b",
                    TargetSelectorType = new ArazzoCriterionExpressionType
                    {
                        Type = ArazzoCriterionExpressionTypeType.XPath,
                        Version = ArazzoCriterionExpressionVersion.XPath20
                    },
                    Value = JsonValue.Create(42)
                }
            }
        };
        using var textWriter = new StringWriter();
        Serialize(body, version, new OpenApiYamlWriter(textWriter));

        var yaml = textWriter.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        var propertyName = GetPropertyName(version);
        Assert.Contains($"{propertyName}: jsonpointer", yaml, StringComparison.Ordinal);
        Assert.Contains($"{propertyName}:\n      type: xpath\n      version: xpath-20", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpressionType:", yaml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void PayloadReplacement_RejectsMismatchedExpressionTypeVersion(ArazzoSpecVersion version)
    {
        var propertyName = GetPropertyName(version);
        var json = JsonNode.Parse($$$"""{"target":"/id","value":"replacement","{{{propertyName}}}":{"type":"jsonpath","version":"xpath-20"}}""");
        Assert.NotNull(json);
        var context = new ParsingContext(new());

        var replacement = context.ParseFragment<ArazzoPayloadReplacement>(json, version);

        Assert.NotNull(replacement);
        Assert.NotNull(replacement.TargetSelectorType);
        Assert.NotNull(replacement.TargetSelectorType.ExpressionType);
        Assert.Null(replacement.TargetSelectorType.Type);
        var error = Assert.Single(context.Diagnostic.Errors);
        Assert.Equal($"#/{propertyName}", error.Pointer);
        Assert.Contains("version 'xpath-20' is not valid for type 'jsonpath'", error.Message, StringComparison.Ordinal);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(replacement, version));
    }

    private static string GetPropertyName(ArazzoSpecVersion version) =>
        version is ArazzoSpecVersion.Arazzo1_0
            ? ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoPayloadReplacementTargetSelectorType)
            : ArazzoConstants.ArazzoPayloadReplacementTargetSelectorType;

    private static string Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var textWriter = new StringWriter();
        Serialize(value, version, new OpenApiJsonWriter(textWriter));
        return textWriter.ToString();
    }

    private static void Serialize(IArazzoSerializable value, ArazzoSpecVersion version, IOpenApiWriter writer)
    {
        if (version is ArazzoSpecVersion.Arazzo1_0)
        {
            value.SerializeAsV1(writer);
        }
        else
        {
            value.SerializeAsV1_1(writer);
        }
    }
}