using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoParameterUnionTests
{
    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void WorkflowParameters_RoundTripBothBranches(ArazzoSpecVersion version)
    {
        var parameter = new ArazzoParameter { Name = "id", Value = "literal" };
        var reference = new ArazzoParameterReference("shared") { Value = JsonValue.Create("override") };
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "test",
            Steps = [new() { StepId = "s", WorkflowId = "other" }],
            Parameters = [parameter, reference]
        };
        var json = Serialize(workflow, version);
        Assert.Equal("id", json["parameters"]?[0]?["name"]?.GetValue<string>());
        Assert.Equal("$components.parameters.shared", json["parameters"]?[1]?["reference"]?.GetValue<string>());
        Assert.Null(json["parameters"]?[0]?["Parameter"]);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Parameters);
        Assert.NotNull(parsed.Parameters[0].Parameter);
        Assert.NotNull(parsed.Parameters[1].Reference);
        Assert.Equal("override", parsed.Parameters[1].Reference?.Value?.Any?.GetValue<string>());
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
    }

    [Fact]
    public void ReferenceOverrides_PreserveLiteralObjectsAndExplicitNull()
    {
        foreach (var version in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
        {
            foreach (var json in new[] { """{"id":42}""", "null" })
            {
                var input = JsonNode.Parse($$"""{"reference":"$components.parameters.shared","value":{{json}}}""");
                Assert.NotNull(input);
                var context = new ParsingContext(new());

                var parameter = context.ParseFragment<ArazzoParameterUnion>(input, version);

                Assert.NotNull(parameter);
                Assert.NotNull(parameter.Reference);
                Assert.NotNull(parameter.Value);
                Assert.Same(parameter.Reference.Value, parameter.Value);
                Assert.NotNull(parameter.Value.Any);
                Assert.Null(parameter.Value.Expression);
                Assert.Null(parameter.Value.Selector);
                if (json == "null") Assert.True(parameter.Value.Any.IsJsonNullSentinel());
                Assert.Empty(context.Diagnostic.Errors);
                Assert.True(JsonNode.DeepEquals(input, Serialize(parameter, version)));
            }
        }
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$message.payload#/id")]
    public void ReferenceOverrides_PreserveTypedExpressions(ArazzoSpecVersion version, string expression)
    {
        var input = JsonNode.Parse($$"""{"reference":"$components.parameters.shared","value":"{{expression}}"}""");
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var parameter = context.ParseFragment<ArazzoParameterUnion>(input, version);

        Assert.NotNull(parameter);
        Assert.NotNull(parameter.Value);
        Assert.NotNull(parameter.Value.Expression);
        Assert.Equal(expression, parameter.Value.Expression.Expression);
        Assert.Null(parameter.Value.Any);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(input, Serialize(parameter, version)));
        if (version is ArazzoSpecVersion.Arazzo1_1)
        {
            Assert.Throws<ArazzoSerializationException>(() => Serialize(parameter, ArazzoSpecVersion.Arazzo1_0));
        }
    }

    [Fact]
    public void ReferenceOverrides_PreserveSelectorsAndRejectUnsupportedVersions()
    {
        var input = JsonNode.Parse("""
            {"reference":"$components.parameters.shared","value":{"context":"$response.body","selector":"$.id","type":"jsonpath"}}
            """);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var parameter = context.ParseFragment<ArazzoParameterUnion>(input, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(parameter);
        Assert.NotNull(parameter.Value);
        Assert.NotNull(parameter.Value.Selector);
        Assert.Null(parameter.Value.Any);
        Assert.Null(parameter.Value.Expression);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(input, Serialize(parameter, ArazzoSpecVersion.Arazzo1_1)));
        Assert.Throws<ArazzoSerializationException>(() => Serialize(parameter, ArazzoSpecVersion.Arazzo1_0));
        var v1Context = new ParsingContext(new());
        _ = v1Context.ParseFragment<ArazzoParameterUnion>(input, ArazzoSpecVersion.Arazzo1_0);
        Assert.Contains(v1Context.Diagnostic.Errors, error => error.Message.Contains("not supported in Arazzo 1.0", StringComparison.Ordinal));
    }

    [Fact]
    public void ReferenceOverrides_RejectEmptyAndConflictingValues()
    {
        var reference = new ArazzoParameterReference("shared") { Value = new ArazzoParameterValueUnion() };
        Assert.Throws<ArazzoSerializationException>(() => Serialize(reference, ArazzoSpecVersion.Arazzo1_1));
        reference.Value.Any = JsonValue.Create("literal");
        reference.Value.Expression = new("$inputs.id");
        Assert.Throws<ArazzoSerializationException>(() => Serialize(reference, ArazzoSpecVersion.Arazzo1_0));
        Assert.Throws<ArazzoSerializationException>(() => Serialize(reference, ArazzoSpecVersion.Arazzo1_1));
    }

    [Fact]
    public void Conversions_PreserveIdentityAndRejectInvalidStates()
    {
        var parameter = new ArazzoParameter { Name = "id", Value = "value" };
        var reference = new ArazzoParameterReference("shared");
        ArazzoParameterUnion value = parameter;
        ArazzoParameter actual = value;
        Assert.Same(parameter, actual);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameterReference)value);
        value = reference;
        ArazzoParameterReference actualReference = value;
        Assert.Same(reference, actualReference);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameter)value);
        value.Parameter = parameter;
        Assert.Throws<InvalidCastException>(() => (ArazzoParameter)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameterReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        value = new();
        Assert.Throws<InvalidCastException>(() => (ArazzoParameter)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameterReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterUnion)(ArazzoParameter)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterUnion)(ArazzoParameterReference)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameter)(ArazzoParameterUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterReference)(ArazzoParameterUnion)null!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Reader_RejectsConflictingBranches(ArazzoSpecVersion version)
    {
        var json = JsonNode.Parse("""{"reference":"$components.parameters.shared","name":"id","value":"override"}""");
        Assert.NotNull(json);
        Assert.Throws<ArazzoReaderException>(() => new ParsingContext(new()).ParseFragment<ArazzoParameterUnion>(json, version));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void StepParameters_RoundTripBothBranchesAndValidateLocation(ArazzoSpecVersion version)
    {
        var parameter = new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "literal" };
        var step = new ArazzoStep
        {
            StepId = "s",
            OperationId = "op",
            Parameters = [parameter, new ArazzoParameterReference("shared")]
        };
        var json = Serialize(step, version);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoStep>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Parameters);
        Assert.NotNull(parsed.Parameters[0].Parameter);
        Assert.NotNull(parsed.Parameters[1].Reference);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
        parameter.In = null;
        Assert.Throws<ArazzoSerializationException>(() => Serialize(step, version));
    }

    private static JsonNode Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        if (version is ArazzoSpecVersion.Arazzo1_0) value.SerializeAsV1(writer);
        else value.SerializeAsV1_1(writer);
        var node = JsonNode.Parse(text.ToString());
        Assert.NotNull(node);
        return node;
    }
}