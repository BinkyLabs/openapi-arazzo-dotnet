using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoOutputValueUnionTests
{
    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void WorkflowExpressions_RoundTripInline(ArazzoSpecVersion version)
    {
        var expression = new ArazzoRuntimeExpression("$inputs.id");
        ArazzoOutputValueUnion output = expression;
        ArazzoRuntimeExpression actual = output;
        Assert.Same(expression, actual);
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "test",
            Steps = [new() { StepId = "s", OperationId = "op" }],
            Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = output }
        };
        var json = Serialize(workflow, version);
        Assert.Equal("$inputs.id", json["outputs"]?["id"]?.GetValue<string>());
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Outputs);
        Assert.Equal("$inputs.id", parsed.Outputs["id"].Expression?.Expression);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
    }

    [Fact]
    public void WorkflowSelectors_RoundTripAndRejectV1()
    {
        var selector = new ArazzoSelector { Context = "$response.body", Selector = "/id", Type = ArazzoPayloadRequirementTargetSelectorType.JsonPointer };
        ArazzoOutputValueUnion value = selector;
        ArazzoSelector actual = value;
        Assert.Same(selector, actual);
        var workflow = new ArazzoWorkflow { WorkflowId = "test", Steps = [new() { StepId = "s", OperationId = "op" }], Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = value } };
        var json = Serialize(workflow, ArazzoSpecVersion.Arazzo1_1);
        Assert.IsType<JsonObject>(json["outputs"]?["id"]);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoWorkflow>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Outputs);
        Assert.NotNull(parsed.Outputs["id"].Selector);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(workflow, ArazzoSpecVersion.Arazzo1_0));
        var v1 = new ParsingContext(new());
        v1.ParseFragment<ArazzoWorkflow>(json, ArazzoSpecVersion.Arazzo1_0);
        Assert.Contains(v1.Diagnostic.Errors, e => e.Message.Contains("not supported", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"literal\"")]
    public void WorkflowInvalidOutputs_ReportLocatedErrors(string value)
    {
        var json = JsonNode.Parse("""{"workflowId":"test","steps":[{"stepId":"s","operationId":"op"}],"outputs":{"bad":""" + value + "}}");
        Assert.NotNull(json);
        var context = new ParsingContext(new());
        context.ParseFragment<ArazzoWorkflow>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.Contains(context.Diagnostic.Errors, e => e.Pointer?.Contains("outputs/bad", StringComparison.Ordinal) is true);
    }

    [Fact]
    public void ConversionsAndConflicts_AreChecked()
    {
        ArazzoOutputValueUnion value = "$inputs.id";
        Assert.Throws<InvalidCastException>(() => (ArazzoSelector)value);
        value.Selector = new();
        Assert.Throws<InvalidCastException>(() => (ArazzoRuntimeExpression)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoSelector)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        Assert.Throws<ArazzoSerializationException>(() => Serialize(new ArazzoOutputValueUnion(), ArazzoSpecVersion.Arazzo1_1));
        Assert.Throws<ArgumentNullException>(() => (ArazzoRuntimeExpression)(ArazzoOutputValueUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSelector)(ArazzoOutputValueUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoOutputValueUnion)(ArazzoRuntimeExpression)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoOutputValueUnion)(ArazzoSelector)null!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void StepOutputs_RoundTripAndRejectUnsupportedSelectors(ArazzoSpecVersion version)
    {
        var step = new ArazzoStep
        {
            StepId = "s",
            OperationId = "op",
            Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = "$inputs.id" }
        };
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoStep>(Serialize(step, version), version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Outputs);
        Assert.Equal("$inputs.id", parsed.Outputs["id"].Expression?.Expression);
        Assert.Empty(context.Diagnostic.Errors);
        step.Outputs["id"] = new ArazzoSelector { Context = "$response.body", Selector = "/id", Type = ArazzoPayloadRequirementTargetSelectorType.JsonPointer };
        var json = Serialize(step, ArazzoSpecVersion.Arazzo1_1);
        parsed = context.ParseFragment<ArazzoStep>(json, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Outputs);
        Assert.NotNull(parsed.Outputs["id"].Selector);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(step, ArazzoSpecVersion.Arazzo1_0));
        var v1 = new ParsingContext(new());
        v1.ParseFragment<ArazzoStep>(json, ArazzoSpecVersion.Arazzo1_0);
        Assert.Contains(v1.Diagnostic.Errors, e => e.Pointer?.Contains("outputs/id", StringComparison.Ordinal) is true && e.Message.Contains("not supported", StringComparison.Ordinal));
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