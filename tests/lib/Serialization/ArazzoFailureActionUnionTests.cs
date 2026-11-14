using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoFailureActionUnionTests
{
    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void WorkflowActions_RoundTripBothBranchesAndCheckDuplicateReferences(ArazzoSpecVersion version)
    {
        var action = new ArazzoFailureAction { Name = "retry", Type = ArazzoFailureType.Retry, RetryLimit = 4, RetryAfter = 1.5m };
        var reference = new ArazzoFailureActionReference("shared");
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "test",
            Steps = [new() { StepId = "s", OperationId = "op" }],
            FailureActions = [action, reference]
        };
        var json = Serialize(workflow, version);
        Assert.Equal("retry", json["failureActions"]?[0]?["name"]?.GetValue<string>());
        Assert.Equal("$components.failureActions.shared", json["failureActions"]?[1]?["reference"]?.GetValue<string>());
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.FailureActions);
        Assert.NotNull(parsed.FailureActions[0].FailureAction);
        Assert.NotNull(parsed.FailureActions[1].Reference);
        Assert.Equal(4UL, parsed.FailureActions[0].RetryLimit);
        Assert.Equal(1.5m, parsed.FailureActions[0].RetryAfter);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
        workflow.FailureActions.Add(reference);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(workflow, version));
        json["failureActions"]?.AsArray().Add(json["failureActions"]?[1]?.DeepClone());
        context = new(new());
        context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.Contains(context.Diagnostic.Errors, e => e.Message.Contains("duplicate action", StringComparison.Ordinal));
    }

    [Fact]
    public void Conversions_PreserveIdentityAndRejectInvalidStates()
    {
        var action = new ArazzoFailureAction { Name = "finish", Type = ArazzoFailureType.End };
        var reference = new ArazzoFailureActionReference("shared");
        ArazzoFailureActionUnion value = action;
        ArazzoFailureAction actual = value;
        Assert.Same(action, actual);
        Assert.Throws<InvalidCastException>(() => (ArazzoFailureActionReference)value);
        value = reference;
        ArazzoFailureActionReference actualReference = value;
        Assert.Same(reference, actualReference);
        Assert.Throws<InvalidCastException>(() => (ArazzoFailureAction)value);
        value.FailureAction = action;
        Assert.Throws<InvalidCastException>(() => (ArazzoFailureAction)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoFailureActionReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        value = new();
        Assert.Throws<InvalidCastException>(() => (ArazzoFailureAction)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoFailureActionReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Throws<ArgumentNullException>(() => (ArazzoFailureActionUnion)(ArazzoFailureAction)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoFailureActionUnion)(ArazzoFailureActionReference)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoFailureAction)(ArazzoFailureActionUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoFailureActionReference)(ArazzoFailureActionUnion)null!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Reader_RejectsConflictingBranchesAndNonObjects(ArazzoSpecVersion version)
    {
        var json = JsonNode.Parse("""{"reference":"$components.failureActions.shared","name":"finish","type":"end"}""");
        Assert.NotNull(json);
        Assert.Throws<ArazzoReaderException>(() => new ParsingContext(new()).ParseFragment<ArazzoFailureActionUnion>(json, version));
        json = JsonNode.Parse("""{"workflowId":"test","steps":[{"stepId":"s","operationId":"op"}],"failureActions":[42]}""");
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
        var reference = new ArazzoFailureActionReference("shared");
        var step = new ArazzoStep
        {
            StepId = "s",
            OperationId = "op",
            OnFailure = [new ArazzoFailureAction { Name = "retry", Type = ArazzoFailureType.Retry, RetryLimit = 4 }, reference]
        };
        var json = Serialize(step, version);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoStep>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.OnFailure);
        Assert.NotNull(parsed.OnFailure[0].FailureAction);
        Assert.NotNull(parsed.OnFailure[1].Reference);
        Assert.Equal(4UL, parsed.OnFailure[0].RetryLimit);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
        step.OnFailure.Add(reference);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(step, version));
        json["onFailure"]?.AsArray().Add(json["onFailure"]?[1]?.DeepClone());
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