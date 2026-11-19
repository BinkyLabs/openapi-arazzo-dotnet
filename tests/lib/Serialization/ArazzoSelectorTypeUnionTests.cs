using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoSelectorTypeUnionTests
{
    [Theory]
    [InlineData(null, "$.items[?@.id == 1].name", true)]
    [InlineData("rfc9535", "$..items[0,2:4]", true)]
    [InlineData(null, "$.items[?length(@.name) > 2]", true)]
    [InlineData(null, "/items", false)]
    [InlineData("rfc9535", "$.items[", false)]
    [InlineData(null, "$.id == 1", false)]
    [InlineData(null, "$.items[?@.id + 1 == 2]", false)]
    [InlineData(null, "@.id", false)]
    [InlineData("draft-goessner-dispatch-jsonpath-00", "$.items[(@.length-1)]", true)]
    public void JsonPathSelectors_ValidateGrammarDuringReadingAndWriting(string? version, string expression, bool valid)
    {
        JsonNode type = version is null ? JsonValue.Create("jsonpath") :
            new JsonObject { ["type"] = "jsonpath", ["version"] = version };
        var json = new JsonObject { ["context"] = "$response.body", ["selector"] = expression, ["type"] = type };
        var context = new ParsingContext(new());
        var selector = context.ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(selector);
        using var text = new StringWriter();
        if (valid)
        {
            Assert.Empty(context.Diagnostic.Errors);
            selector.SerializeAsV1_1(new OpenApiJsonWriter(text));
        }
        else
        {
            var error = Assert.Single(context.Diagnostic.Errors);
            Assert.Contains("RFC 9535", error.Message, StringComparison.Ordinal);
            Assert.Throws<ArazzoSerializationException>(() => selector.SerializeAsV1_1(new OpenApiJsonWriter(text)));
        }
    }

    [Fact]
    public void ReplacementTargets_ValidateExplicitJsonPathGrammar()
    {
        var json = new JsonObject { ["target"] = "$.items[", ["targetSelectorType"] = "jsonpath", ["value"] = 1 };
        var context = new ParsingContext(new());
        var replacement = context.ParseFragment<ArazzoPayloadReplacement>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(replacement);
        Assert.Single(context.Diagnostic.Errors);
        using var text = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => replacement.SerializeAsV1_1(new OpenApiJsonWriter(text)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("xpath-10")]
    [InlineData("xpath-20")]
    [InlineData("xpath-30")]
    [InlineData("xpath-31")]
    public void XPathSelectors_PreserveDeferredGrammarValidation(string? version)
    {
        JsonNode type = version is null ? JsonValue.Create("xpath") :
            new JsonObject { ["type"] = "xpath", ["version"] = version };
        var json = new JsonObject { ["context"] = "$response.body", ["selector"] = "/items[", ["type"] = type };
        var context = new ParsingContext(new());
        var selector = context.ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(selector);
        Assert.Empty(context.Diagnostic.Errors);
        using var text = new StringWriter();
        selector.SerializeAsV1_1(new OpenApiJsonWriter(text));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("/", true)]
    [InlineData("/a~1b/~0", true)]
    [InlineData("/a~01/%2F", true)]
    [InlineData("not-a-pointer", false)]
    [InlineData("#/id", false)]
    [InlineData("/bad~2", false)]
    [InlineData("/trailing~", false)]
    public void JsonPointerSelectors_ValidateSyntaxDuringReadingAndWriting(string pointer, bool valid)
    {
        var json = new JsonObject { ["context"] = "$response.body", ["selector"] = pointer, ["type"] = "jsonpointer" };
        var context = new ParsingContext(new());
        var selector = context.ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(selector);
        using var text = new StringWriter();
        if (valid)
        {
            Assert.Empty(context.Diagnostic.Errors);
            selector.SerializeAsV1_1(new OpenApiJsonWriter(text));
        }
        else
        {
            Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("RFC 6901", StringComparison.Ordinal));
            Assert.Throws<ArazzoSerializationException>(() => selector.SerializeAsV1_1(new OpenApiJsonWriter(text)));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidReplacementPointers_AreRejectedWithExplicitAndDefaultTypes(bool explicitType)
    {
        var replacement = new JsonObject { ["target"] = "not-a-pointer", ["value"] = 1 };
        if (explicitType)
            replacement["targetSelectorType"] = "jsonpointer";
        var json = new JsonObject
        {
            ["contentType"] = "application/json",
            ["payload"] = new JsonObject { ["id"] = 0 },
            ["replacements"] = new JsonArray(replacement)
        };
        var context = new ParsingContext(new());
        var body = context.ParseFragment<ArazzoRequestBody>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(body);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("RFC 6901", StringComparison.Ordinal));
        using var text = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => body.SerializeAsV1_1(new OpenApiJsonWriter(text)));
    }

    [Theory]
    [InlineData("jsonpath", ArazzoPayloadRequirementTargetSelectorType.JsonPath)]
    [InlineData("xpath", ArazzoPayloadRequirementTargetSelectorType.XPath)]
    [InlineData("jsonpointer", ArazzoPayloadRequirementTargetSelectorType.JsonPointer)]
    public void Languages_ConvertAndRoundTrip(string name, ArazzoPayloadRequirementTargetSelectorType language)
    {
        ArazzoSelectorTypeUnion value = language;
        ArazzoPayloadRequirementTargetSelectorType actual = value;
        Assert.Equal(language, actual);
        var selector = new ArazzoSelector { Context = new("$response.body"), Selector = language is ArazzoPayloadRequirementTargetSelectorType.JsonPath ? "$.id" : "/id", Type = value };
        using var text = new StringWriter();
        selector.SerializeAsV1_1(new OpenApiJsonWriter(text));
        var json = JsonNode.Parse(text.ToString());
        Assert.NotNull(json);
        Assert.Equal(name, json["type"]?.GetValue<string>());
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Type);
        Assert.Equal(language, parsed.Type.Type);
        Assert.Null(parsed.Type.ExpressionType);
        Assert.Empty(context.Diagnostic.Errors);
        using var v1Text = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => selector.SerializeAsV1(new OpenApiJsonWriter(v1Text)));
        Assert.Throws<ArazzoReaderException>(() => new ParsingContext(new()).ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_0));
    }

    [Fact]
    public void ObjectBranchAndInvalidStates_ConvertAndValidate()
    {
        var type = new ArazzoCriterionExpressionType { Type = ArazzoCriterionExpressionTypeType.JsonPointer, Version = ArazzoCriterionExpressionVersion.Rfc6901 };
        ArazzoSelectorTypeUnion value = type;
        ArazzoCriterionExpressionType actual = value;
        Assert.Same(type, actual);
        Assert.Throws<InvalidCastException>(() => (ArazzoPayloadRequirementTargetSelectorType)value);
        value.Type = ArazzoPayloadRequirementTargetSelectorType.JsonPath;
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionType)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoPayloadRequirementTargetSelectorType)value);
        using var conflictingText = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => value.SerializeAsV1_1(new OpenApiJsonWriter(conflictingText)));
        using var emptyText = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => new ArazzoSelectorTypeUnion().SerializeAsV1_1(new OpenApiJsonWriter(emptyText)));
        Assert.Throws<ArgumentNullException>(() => (ArazzoCriterionExpressionType)(ArazzoSelectorTypeUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoPayloadRequirementTargetSelectorType)(ArazzoSelectorTypeUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSelectorTypeUnion)(ArazzoCriterionExpressionType)null!);
    }

    [Fact]
    public void Selector_RejectsMismatchedExpressionTypeVersion()
    {
        var json = JsonNode.Parse("""{"context":"$response.body","selector":"$.id","type":{"type":"jsonpath","version":"xpath-20"}}""");
        Assert.NotNull(json);
        var context = new ParsingContext(new());

        var selector = context.ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(selector);
        Assert.NotNull(selector.Type);
        Assert.NotNull(selector.Type.ExpressionType);
        Assert.Null(selector.Type.Type);
        var error = Assert.Single(context.Diagnostic.Errors);
        Assert.Equal("#/type", error.Pointer);
        Assert.Contains("version 'xpath-20' is not valid for type 'jsonpath'", error.Message, StringComparison.Ordinal);
        using var text = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => selector.SerializeAsV1_1(new OpenApiJsonWriter(text)));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("\"simple\"")]
    public void InvalidShapes_ReportDiagnostics(string type)
    {
        var json = JsonNode.Parse($$"""{"context":"$response.body","selector":"/id","type":{{type}}}""");
        Assert.NotNull(json);
        var context = new ParsingContext(new());
        context.ParseFragment<ArazzoSelector>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotEmpty(context.Diagnostic.Errors);
    }
}