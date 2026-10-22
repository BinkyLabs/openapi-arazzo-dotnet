using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoCriterionTypeUnionTests
{
    [Theory]
    [InlineData("simple", ArazzoCriterionExpressionTypeType.Simple)]
    [InlineData("regex", ArazzoCriterionExpressionTypeType.Regex)]
    [InlineData("jsonpath", ArazzoCriterionExpressionTypeType.JsonPath)]
    [InlineData("xpath", ArazzoCriterionExpressionTypeType.XPath)]
    public void TypeNames_RoundTripBothVersions(string name, ArazzoCriterionExpressionTypeType type)
    {
        foreach (var version in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
        {
            ArazzoCriterionTypeUnion value = type;
            ArazzoCriterionExpressionTypeType actual = value;
            Assert.Equal(type, actual);
            Assert.Equal($"\"{name}\"", Serialize(value, version));
            var context = new ParsingContext(new());
            var parsed = context.ParseFragment<ArazzoCriterionTypeUnion>(JsonValue.Create(name), version);
            Assert.NotNull(parsed);
            Assert.Equal(type, parsed.Type);
            Assert.Null(parsed.ExpressionType);
            Assert.Empty(context.Diagnostic.Errors);
        }
    }

    [Fact]
    public void Objects_PreserveIdentityAndWireShapeAndEnforceVersion()
    {
        var expressionType = new ArazzoCriterionExpressionType { Type = ArazzoCriterionExpressionTypeType.JsonPointer, Version = ArazzoCriterionExpressionVersion.Rfc6901 };
        ArazzoCriterionTypeUnion value = expressionType;
        ArazzoCriterionExpressionType actual = value;
        Assert.Same(expressionType, actual);
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionTypeType)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        var json = JsonNode.Parse(Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"type":"jsonpointer","version":"rfc6901"}"""), json));
        Assert.NotNull(json);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoCriterionTypeUnion>(json, ArazzoSpecVersion.Arazzo1_0);
        Assert.NotNull(parsed);
        Assert.Contains(context.Diagnostic.Errors, e => e.Message.Contains("not supported", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"unknown\"")]
    [InlineData("\"jsonpointer\"")]
    public void InvalidShapes_AreReported(string json)
    {
        var node = JsonNode.Parse($$"""{"context":"$response.body","condition":"true","type":{{json}}}""");
        Assert.NotNull(node);
        var context = new ParsingContext(new());
        context.ParseFragment<ArazzoCriterion>(node, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotEmpty(context.Diagnostic.Errors);
    }

    [Fact]
    public void InvalidStatesAndConversions_Throw()
    {
        var value = new ArazzoCriterionTypeUnion();
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionTypeType)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionType)value);
        value.Type = ArazzoCriterionExpressionTypeType.JsonPointer;
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        value.ExpressionType = new();
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionTypeType)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoCriterionExpressionType)value);
        Assert.Throws<ArgumentNullException>(() => (ArazzoCriterionTypeUnion)(ArazzoCriterionExpressionType)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoCriterionExpressionType)(ArazzoCriterionTypeUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoCriterionExpressionTypeType)(ArazzoCriterionTypeUnion)null!);
    }

    private static string Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        if (version is ArazzoSpecVersion.Arazzo1_0) value.SerializeAsV1(writer);
        else value.SerializeAsV1_1(writer);
        return text.ToString();
    }
}