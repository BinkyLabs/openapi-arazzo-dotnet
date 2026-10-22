using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoSelectorTypeUnionTests
{
    [Theory]
    [InlineData("jsonpath", ArazzoPayloadRequirementTargetSelectorType.JsonPath)]
    [InlineData("xpath", ArazzoPayloadRequirementTargetSelectorType.XPath)]
    [InlineData("jsonpointer", ArazzoPayloadRequirementTargetSelectorType.JsonPointer)]
    public void Languages_ConvertAndRoundTrip(string name, ArazzoPayloadRequirementTargetSelectorType language)
    {
        ArazzoSelectorTypeUnion value = language;
        ArazzoPayloadRequirementTargetSelectorType actual = value;
        Assert.Equal(language, actual);
        var selector = new ArazzoSelector { Context = "$response.body", Selector = "/id", Type = value };
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
        Assert.Throws<ArazzoSerializationException>(() => selector.SerializeAsV1(new OpenApiJsonWriter(new StringWriter())));
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
        Assert.Throws<ArazzoSerializationException>(() => value.SerializeAsV1_1(new OpenApiJsonWriter(new StringWriter())));
        Assert.Throws<ArazzoSerializationException>(() => new ArazzoSelectorTypeUnion().SerializeAsV1_1(new OpenApiJsonWriter(new StringWriter())));
        Assert.Throws<ArgumentNullException>(() => (ArazzoCriterionExpressionType)(ArazzoSelectorTypeUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoPayloadRequirementTargetSelectorType)(ArazzoSelectorTypeUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSelectorTypeUnion)(ArazzoCriterionExpressionType)null!);
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