// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader.V1;

public class ArazzoV1DeserializerAdditionalTests
{
    private static MapNode ParseToMap(string json, out ParsingContext ctx)
    {
        var jsonNode = JsonNode.Parse(json)!;
        ctx = new ParsingContext(new ArazzoDiagnostic());
        return new MapNode(ctx, jsonNode);
    }

    [Fact]
    public void LoadComponent_WithInputsRef_LoadsSchemaReference()
    {
        var json = """
            {
              "inputs": {
                "MyInput": { "$ref": "#/components/schemas/Foo" }
              }
            }
            """;
        var map = ParseToMap(json, out _);

        var component = ArazzoV1Deserializer.LoadComponent(map);

        Assert.NotNull(component);
    }

    [Fact]
    public void LoadComponent_WithInputsInlineSchema_LoadsSchema()
    {
        var json = """
            {
              "inputs": {
                "MyInput": { "type": "string" }
              }
            }
            """;
        var map = ParseToMap(json, out _);

        var component = ArazzoV1Deserializer.LoadComponent(map);

        Assert.NotNull(component);
    }

    [Fact]
    public void LoadComponent_WithSuccessAndFailureActions()
    {
        var json = """
            {
              "successActions": {
                "OnOk": { "name": "OnOk", "type": "end" }
              },
              "failureActions": {
                "OnErr": { "name": "OnErr", "type": "end" }
              },
              "parameters": {
                "P": { "name": "p", "in": "header", "value": "v" }
              },
              "x-ext": "value"
            }
            """;
        var map = ParseToMap(json, out _);

        var component = ArazzoV1Deserializer.LoadComponent(map);

        Assert.NotNull(component.SuccessActions);
        Assert.True(component.SuccessActions!.ContainsKey("OnOk"));
        Assert.NotNull(component.FailureActions);
        Assert.True(component.FailureActions!.ContainsKey("OnErr"));
        Assert.NotNull(component.Parameters);
        Assert.NotNull(component.Extensions);
    }

    [Fact]
    public void LoadWorkflow_WithInputsSchema_AndDependsOn()
    {
        var json = """
            {
              "workflowId": "wf",
              "summary": "s",
              "inputs": { "type": "object" },
              "dependsOn": ["a", "b"],
              "steps": [
                { "stepId": "s1", "operationId": "op1" }
              ],
              "successActions": [ { "name": "ok", "type": "end" } ],
              "failureActions": [ { "name": "err", "type": "end" } ],
              "outputs": { "k": "$.v" },
              "parameters": { "p1": { "name": "p1", "in": "query", "value": "v" } },
              "x-extra": "wf-ext"
            }
            """;
        var map = ParseToMap(json, out _);

        var workflow = ArazzoV1Deserializer.LoadWorkflow(map);

        Assert.Equal("wf", workflow.WorkflowId);
        Assert.Equal("s", workflow.Summary);
        Assert.NotNull(workflow.Inputs);
        Assert.NotNull(workflow.DependsOn);
        Assert.Equal(2, workflow.DependsOn!.Count);
        Assert.Single(workflow.Steps!);
        Assert.NotNull(workflow.SuccessActions);
        Assert.NotNull(workflow.FailureActions);
        Assert.NotNull(workflow.Outputs);
        Assert.NotNull(workflow.Parameters);
        Assert.NotNull(workflow.Extensions);
    }

    [Fact]
    public void LoadWorkflow_EmptyDependsOn_NoHashSetCreated()
    {
        var json = """
            {
              "workflowId": "wf",
              "dependsOn": []
            }
            """;
        var map = ParseToMap(json, out _);

        var workflow = ArazzoV1Deserializer.LoadWorkflow(map);

        Assert.Null(workflow.DependsOn);
    }

    [Fact]
    public void LoadStep_WithAllFields_LoadsCorrectly()
    {
        var json = """
            {
              "description": "d",
              "stepId": "s",
              "operationId": "op",
              "operationPath": "$.op",
              "workflowId": "wf",
              "parameters": [{ "name": "p", "in": "query", "value": "v" }],
              "requestBody": { "contentType": "application/json", "payload": "abc" },
              "successCriteria": [{ "condition": "true" }],
              "onSuccess": [{ "name": "next", "type": "end" }],
              "onFailure": [{ "name": "stop", "type": "end" }],
              "outputs": { "k": "$.v" },
              "x-step-ext": "value"
            }
            """;
        var map = ParseToMap(json, out _);

        var step = ArazzoV1Deserializer.LoadStep(map);

        Assert.Equal("d", step.Description);
        Assert.Equal("s", step.StepId);
        Assert.Equal("op", step.OperationId);
        Assert.Equal("$.op", step.OperationPath);
        Assert.Equal("wf", step.WorkflowId);
        Assert.NotNull(step.Parameters);
        Assert.NotNull(step.RequestBody);
        Assert.NotNull(step.SuccessCriteria);
        Assert.NotNull(step.OnSuccess);
        Assert.NotNull(step.OnFailure);
        Assert.NotNull(step.Outputs);
        Assert.NotNull(step.Extensions);
    }

    [Fact]
    public void LoadSuccessAction_UnknownType_RecordsDiagnostic()
    {
        var json = """{ "name": "n", "type": "bogus" }""";
        var map = ParseToMap(json, out var ctx);

        var action = ArazzoV1Deserializer.LoadSuccessAction(map);

        Assert.Null(action.Type);
        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("not recognized"));
    }

    [Fact]
    public void LoadFailureAction_AllFields()
    {
        var json = """
            {
              "name": "n",
              "type": "retry",
              "workflowId": "wf",
              "stepId": "s",
              "retryAfter": "1.5",
              "retryLimit": "3",
              "criteria": [{"condition": "true"}]
            }
            """;
        var map = ParseToMap(json, out _);

        var action = ArazzoV1Deserializer.LoadFailureAction(map);

        Assert.Equal("n", action.Name);
        Assert.Equal(ArazzoFailureType.Retry, action.Type);
        Assert.Equal("wf", action.WorkflowId);
        Assert.Equal("s", action.StepId);
        Assert.Equal(1.5m, action.RetryAfter);
        Assert.Equal(3UL, action.RetryLimit);
        Assert.NotNull(action.Criteria);
    }

    [Fact]
    public void LoadFailureAction_InvalidRetryValues_LeavesNull()
    {
        var json = """
            {
              "name": "n",
              "type": "retry",
              "retryAfter": "not-a-number",
              "retryLimit": "not-a-number"
            }
            """;
        var map = ParseToMap(json, out _);

        var action = ArazzoV1Deserializer.LoadFailureAction(map);

        Assert.Null(action.RetryAfter);
        Assert.Null(action.RetryLimit);
    }

    [Fact]
    public void LoadFailureAction_UnknownType_RecordsDiagnostic()
    {
        var json = """{ "name": "n", "type": "weird" }""";
        var map = ParseToMap(json, out var ctx);

        var action = ArazzoV1Deserializer.LoadFailureAction(map);

        Assert.Null(action.Type);
        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("not recognized"));
    }

    [Fact]
    public void LoadInfo_MissingFields_LoadsWithNulls()
    {
        var json = """{ "x-info-ext": "ext" }""";
        var map = ParseToMap(json, out _);

        var info = ArazzoV1Deserializer.LoadInfo(map);

        Assert.Null(info.Title);
        Assert.Null(info.Version);
        Assert.NotNull(info.Extensions);
    }

    [Fact]
    public void LoadPayloadReplacement_AllFields()
    {
        var json = """{ "target": "$.body", "value": "new-value", "x-ext": "v" }""";
        var map = ParseToMap(json, out _);

        var pr = ArazzoV1Deserializer.LoadPayloadReplacement(map);

        Assert.Equal("$.body", pr.Target);
        Assert.NotNull(pr.Value);
        Assert.NotNull(pr.Extensions);
    }

    [Fact]
    public void LoadRequestBody_AllFields()
    {
        var json = """
            {
              "contentType": "application/json",
              "payload": { "k": "v" },
              "replacements": [ { "target": "$.x", "value": 1 } ]
            }
            """;
        var map = ParseToMap(json, out _);

        var rb = ArazzoV1Deserializer.LoadRequestBody(map);

        Assert.Equal("application/json", rb.ContentType);
        Assert.NotNull(rb.Payload);
        Assert.NotNull(rb.Replacements);
        Assert.Single(rb.Replacements!);
    }

    [Fact]
    public void LoadCriterionExpressionType_AllFields()
    {
        var json = """{ "type": "jsonpath", "version": "draft-goessner-dispatch-jsonpath-00" }""";
        var map = ParseToMap(json, out _);

        var expr = ArazzoV1Deserializer.LoadCriterionExpressionType(map);

        Assert.Equal(ArazzoCriterionExpressionTypeType.JsonPath, expr.Type);
        Assert.Equal(ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00, expr.Version);
    }

    [Fact]
    public void LoadCriterionExpressionType_InvalidValues_RecordsDiagnostic()
    {
        var json = """{ "type": "bad", "version": "bad-version" }""";
        var map = ParseToMap(json, out var ctx);

        var expr = ArazzoV1Deserializer.LoadCriterionExpressionType(map);

        Assert.Null(expr.Type);
        Assert.Null(expr.Version);
        Assert.True(ctx.Diagnostic.Errors.Count >= 2);
    }
}