using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoParameterValueUnionTests
{
    [Theory]
    [InlineData("""{"context":"customer"}""")]
    [InlineData("""{"context":"production","id":42}""")]
    [InlineData("""{"context":"production","type":"object"}""")]
    public void ContextOnlyObjects_RemainLiteralInV1(string json)
    {
        var input = JsonNode.Parse($$"""{"name":"input","value":{{json}}}""");
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var parameter = context.ParseFragment<ArazzoParameter>(input, ArazzoSpecVersion.Arazzo1_0);

        Assert.NotNull(parameter);
        Assert.NotNull(parameter.Value);
        Assert.NotNull(parameter.Value.Any);
        Assert.Null(parameter.Value.Selector);
        Assert.Empty(context.Diagnostic.Errors);
        using var text = new StringWriter();
        parameter.SerializeAsV1(new OpenApiJsonWriter(text));
        Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(text.ToString())));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[1,null,{\"id\":2}]")]
    [InlineData("\"literal\"")]
    [InlineData("\"$response.statusCode\"")]
    [InlineData("\"$inputs.id\"")]
    public void Values_RoundTripBothVersions(string json)
    {
        foreach (var version in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
        {
            var node = JsonNode.Parse($$"""{"name":"id","value":{{json}}}""");
            Assert.NotNull(node);
            var context = new ParsingContext(new());
            var parameter = context.ParseFragment<ArazzoParameter>(node, version);
            Assert.NotNull(parameter);
            Assert.NotNull(parameter.Value);
            using var text = new StringWriter();
            var writer = new OpenApiJsonWriter(text);
            if (version is ArazzoSpecVersion.Arazzo1_0) parameter.SerializeAsV1(writer);
            else parameter.SerializeAsV1_1(writer);
            Assert.Empty(context.Diagnostic.Errors);
            Assert.True(JsonNode.DeepEquals(node, JsonNode.Parse(text.ToString())));
            if (json == "\"$inputs.id\"")
            {
                ArazzoRuntimeExpression expression = parameter.Value;
                Assert.Equal("$inputs.id", expression.Expression);
            }
            else
            {
                JsonNode literal = parameter.Value;
                Assert.Same(parameter.Value.Any, literal);
            }
        }
    }

    [Fact]
    public void Objects_UseVersionSpecificSchema()
    {
        var selector = new ArazzoSelector { Context = new("$response.body"), Selector = "/id", Type = ArazzoPayloadRequirementTargetSelectorType.JsonPointer };
        ArazzoParameterValueUnion value = selector;
        ArazzoSelector actual = value;
        Assert.Same(selector, actual);
        using var v1Text = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => value.SerializeAsV1(new OpenApiJsonWriter(v1Text)));
        using var text = new StringWriter();
        value.SerializeAsV1_1(new OpenApiJsonWriter(text));
        var json = JsonNode.Parse(text.ToString());
        Assert.NotNull(json);
        var v1Context = new ParsingContext(new());
        Assert.Throws<ArazzoReaderException>(() => v1Context.ParseFragment<ArazzoParameterValueUnion>(json, ArazzoSpecVersion.Arazzo1_0));
        var context = new ParsingContext(new());
        var v1_1Value = context.ParseFragment<ArazzoParameterValueUnion>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(v1_1Value);
        Assert.NotNull(v1_1Value.Selector);
        Assert.Null(v1_1Value.Any);
        Assert.Empty(context.Diagnostic.Errors);
        ArazzoParameterValueUnion literal = new JsonObject { ["id"] = 42 };
        using var literalText = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => literal.SerializeAsV1_1(new OpenApiJsonWriter(literalText)));
    }

    [Theory]
    [InlineData("$workflows.lookup")]
    [InlineData("$message.payload#/id")]
    public void Interface_PreservesExpressionIdentityWithoutSelectingAnOutputVersion(string expression)
    {
        var value = new ArazzoRuntimeExpression(expression);
        var concrete = new ArazzoParameter { Name = "id", Value = value };
        IArazzoParameter parameter = concrete;
        Assert.Same(concrete.Value, parameter.Value);
        Assert.NotNull(parameter.Value);
        Assert.Same(value, parameter.Value.Expression);
    }

    [Fact]
    public void Conversions_RejectNullWrongAndConflictingMembers()
    {
        var expression = new ArazzoRuntimeExpression("$inputs.id");
        ArazzoParameterValueUnion value = expression;
        ArazzoRuntimeExpression actual = value;
        Assert.Same(expression, actual);
        Assert.Throws<InvalidCastException>(() => (JsonNode)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoSelector)value);
        value.Any = JsonValue.Create(1);
        Assert.Throws<InvalidCastException>(() => (ArazzoRuntimeExpression)value);
        using var conflictingText = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => value.SerializeAsV1_1(new OpenApiJsonWriter(conflictingText)));
        using var emptyText = new StringWriter();
        Assert.Throws<ArazzoSerializationException>(() => new ArazzoParameterValueUnion().SerializeAsV1(new OpenApiJsonWriter(emptyText)));
        Assert.Throws<ArgumentNullException>(() => (JsonNode)(ArazzoParameterValueUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoRuntimeExpression)(ArazzoParameterValueUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSelector)(ArazzoParameterValueUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterValueUnion)(ArazzoRuntimeExpression)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterValueUnion)(ArazzoSelector)null!);
    }
}