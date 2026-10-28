using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoSuccessActionUnionTests
{
    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void WorkflowActions_RoundTripBothBranchesAndCheckDuplicateReferences(ArazzoSpecVersion version)
    {
        var action = new ArazzoSuccessAction { Name = "finish", Type = ArazzoSuccessType.End };
        var reference = new ArazzoSuccessActionReference("shared");
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "test",
            Steps = [new() { StepId = "s", OperationId = "op" }],
            SuccessActions = [action, reference]
        };
        var json = Serialize(workflow, version);
        Assert.Equal("finish", json["successActions"]?[0]?["name"]?.GetValue<string>());
        Assert.Equal("$components.successActions.shared", json["successActions"]?[1]?["reference"]?.GetValue<string>());
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.SuccessActions);
        Assert.NotNull(parsed.SuccessActions[0].SuccessAction);
        Assert.NotNull(parsed.SuccessActions[1].Reference);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
        workflow.SuccessActions.Add(reference);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(workflow, version));
        json["successActions"]?.AsArray().Add(json["successActions"]?[1]?.DeepClone());
        context = new(new());
        context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.Contains(context.Diagnostic.Errors, e => e.Message.Contains("duplicate action", StringComparison.Ordinal));
    }

    [Fact]
    public void Conversions_PreserveIdentityAndRejectInvalidStates()
    {
        var action = new ArazzoSuccessAction { Name = "finish", Type = ArazzoSuccessType.End };
        var reference = new ArazzoSuccessActionReference("shared");
        ArazzoSuccessActionUnion value = action;
        ArazzoSuccessAction actual = value;
        Assert.Same(action, actual);
        Assert.Throws<InvalidCastException>(() => (ArazzoSuccessActionReference)value);
        value = reference;
        ArazzoSuccessActionReference actualReference = value;
        Assert.Same(reference, actualReference);
        Assert.Throws<InvalidCastException>(() => (ArazzoSuccessAction)value);
        value.SuccessAction = action;
        Assert.Throws<InvalidCastException>(() => (ArazzoSuccessAction)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoSuccessActionReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        value = new();
        Assert.Throws<InvalidCastException>(() => (ArazzoSuccessAction)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoSuccessActionReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Throws<ArgumentNullException>(() => (ArazzoSuccessActionUnion)(ArazzoSuccessAction)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSuccessActionUnion)(ArazzoSuccessActionReference)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSuccessAction)(ArazzoSuccessActionUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSuccessActionReference)(ArazzoSuccessActionUnion)null!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Reader_RejectsConflictingBranchesAndNonObjects(ArazzoSpecVersion version)
    {
        var json = JsonNode.Parse("""{"reference":"$components.successActions.shared","name":"finish","type":"end"}""");
        Assert.NotNull(json);
        Assert.Throws<ArazzoReaderException>(() => new ParsingContext(new()).ParseFragment<ArazzoSuccessActionUnion>(json, version));
        json = JsonNode.Parse("""{"workflowId":"test","steps":[{"stepId":"s","operationId":"op"}],"successActions":[42]}""");
        Assert.NotNull(json);
        var context = new ParsingContext(new());
        context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.Contains(context.Diagnostic.Errors, e => e.Message.Contains("Expected object", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void StepActions_RoundTripBothBranchesAndRejectDuplicates(ArazzoSpecVersion version)
    {
        var reference = new ArazzoSuccessActionReference("shared");
        var step = new ArazzoStep
        {
            StepId = "s",
            OperationId = "op",
            OnSuccess = [new ArazzoSuccessAction { Name = "finish", Type = ArazzoSuccessType.End }, reference]
        };
        var json = Serialize(step, version);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoStep>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.OnSuccess);
        Assert.NotNull(parsed.OnSuccess[0].SuccessAction);
        Assert.NotNull(parsed.OnSuccess[1].Reference);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
        step.OnSuccess.Add(reference);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(step, version));
        json["onSuccess"]?.AsArray().Add(json["onSuccess"]?[1]?.DeepClone());
        context = new(new());
        context.ParseFragment<ArazzoStep>(json, version);
        Assert.Contains(context.Diagnostic.Errors, e => e.Message.Contains("duplicate action", StringComparison.Ordinal));
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