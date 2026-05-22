// Licensed under the MIT license.

using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Serialization;

public class SerializationErrorPathTests
{
    private static OpenApiJsonWriter MakeWriter(out StringWriter sw)
    {
        sw = new StringWriter();
        return new OpenApiJsonWriter(sw);
    }

    [Fact]
    public void Parameter_SerializeAsV1_NullWriter_Throws()
    {
        var p = new ArazzoParameter { Name = "n", In = ParameterLocation.Path, Value = JsonValue.Create(1)! };
        Assert.Throws<ArgumentNullException>(() => p.SerializeAsV1(null!));
    }

    [Fact]
    public void Parameter_SerializeAsV1_EmptyName_Throws()
    {
        var p = new ArazzoParameter { Name = "", In = ParameterLocation.Path, Value = JsonValue.Create(1)! };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentException>(() => p.SerializeAsV1(w));
    }

    [Fact]
    public void Parameter_SerializeAsV1_NullIn_Throws()
    {
        var p = new ArazzoParameter { Name = "n", Value = JsonValue.Create(1)! };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentNullException>(() => p.SerializeAsV1(w));
    }

    [Fact]
    public void Parameter_SerializeAsV1_NullValue_Throws()
    {
        var p = new ArazzoParameter { Name = "n", In = ParameterLocation.Path };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentNullException>(() => p.SerializeAsV1(w));
    }

    [Fact]
    public void SourceDescription_SerializeAsV1_NullWriter_Throws()
    {
        var s = new ArazzoSourceDescription { Name = "n", Url = new Uri("https://x") };
        Assert.Throws<ArgumentNullException>(() => s.SerializeAsV1(null!));
    }

    [Fact]
    public void SourceDescription_SerializeAsV1_EmptyName_Throws()
    {
        var s = new ArazzoSourceDescription { Name = "", Url = new Uri("https://x") };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentException>(() => s.SerializeAsV1(w));
    }

    [Fact]
    public void SourceDescription_SerializeAsV1_NullUrl_Throws()
    {
        var s = new ArazzoSourceDescription { Name = "n" };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentNullException>(() => s.SerializeAsV1(w));
    }

    [Fact]
    public void SourceDescription_SerializeAsV1_NoType_OmitsTypeField()
    {
        var s = new ArazzoSourceDescription { Name = "n", Url = new Uri("https://x") };
        var w = MakeWriter(out var sw);
        s.SerializeAsV1(w);
        var json = JsonNode.Parse(sw.ToString())!.AsObject();
        Assert.False(json.ContainsKey("type"));
    }

    [Fact]
    public void Step_SerializeAsV1_NullWriter_Throws()
    {
        var s = new ArazzoStep { StepId = "s" };
        Assert.Throws<ArgumentNullException>(() => s.SerializeAsV1(null!));
    }

    [Fact]
    public void Step_SerializeAsV1_EmptyStepId_Throws()
    {
        var s = new ArazzoStep { StepId = "" };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentException>(() => s.SerializeAsV1(w));
    }

    [Fact]
    public void Step_SerializeAsV1_AllOptionalFieldsSet_WritesAllProperties()
    {
        var step = new ArazzoStep
        {
            StepId = "s1",
            Description = "d",
            OperationId = "op",
            OperationPath = "$.op",
            WorkflowId = "wf",
            Parameters = [new ArazzoParameter { Name = "p", In = ParameterLocation.Query, Value = JsonValue.Create(1)! }],
            RequestBody = new ArazzoRequestBody { ContentType = "application/json", Payload = JsonValue.Create("{}")! },
            SuccessCriteria = [new ArazzoCriterion { Condition = "true" }],
            OnSuccess = [new ArazzoSuccessAction { Name = "n", Type = ArazzoSuccessType.End }],
            OnFailure = [new ArazzoFailureAction { Name = "n", Type = ArazzoFailureType.End }],
            Outputs = new Dictionary<string, string> { ["k"] = "$.v" }
        };
        var w = MakeWriter(out var sw);
        step.SerializeAsV1(w);
        var json = JsonNode.Parse(sw.ToString())!.AsObject();
        Assert.Equal("s1", json["stepId"]!.GetValue<string>());
        Assert.Equal("d", json["description"]!.GetValue<string>());
        Assert.Equal("op", json["operationId"]!.GetValue<string>());
        Assert.Equal("$.op", json["operationPath"]!.GetValue<string>());
        Assert.Equal("wf", json["workflowId"]!.GetValue<string>());
    }

    [Fact]
    public void Criterion_SerializeAsV1_NullWriter_Throws()
    {
        var c = new ArazzoCriterion { Condition = "true" };
        Assert.Throws<ArgumentNullException>(() => c.SerializeAsV1(null!));
    }

    [Fact]
    public void Criterion_SerializeAsV1_EmptyCondition_Throws()
    {
        var c = new ArazzoCriterion { Condition = "" };
        var w = MakeWriter(out _);
        Assert.Throws<ArgumentException>(() => c.SerializeAsV1(w));
    }

    [Fact]
    public void Criterion_SerializeAsV1_SimpleTypeWithVersion_Throws()
    {
        var c = new ArazzoCriterion
        {
            Condition = "x==1",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.Simple,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            }
        };
        var w = MakeWriter(out _);
        Assert.Throws<ArazzoException>(() => c.SerializeAsV1(w));
    }

    [Fact]
    public void Criterion_SerializeAsV1_SimpleTypeNoVersion_WritesString()
    {
        var c = new ArazzoCriterion
        {
            Condition = "x==1",
            Type = new ArazzoCriterionExpressionType { Type = ArazzoCriterionExpressionTypeType.Simple }
        };
        var w = MakeWriter(out var sw);
        c.SerializeAsV1(w);
        var json = JsonNode.Parse(sw.ToString())!.AsObject();
        Assert.Equal("simple", json["type"]!.GetValue<string>());
    }

    [Fact]
    public void Criterion_SerializeAsV1_JsonPathType_WritesObject()
    {
        var c = new ArazzoCriterion
        {
            Condition = "$.a == 1",
            Type = new ArazzoCriterionExpressionType
            {
                Type = ArazzoCriterionExpressionTypeType.JsonPath,
                Version = ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00
            }
        };
        var w = MakeWriter(out var sw);
        c.SerializeAsV1(w);
        var json = JsonNode.Parse(sw.ToString())!.AsObject();
        Assert.NotNull(json["type"]!.AsObject());
    }

    [Fact]
    public void Criterion_SerializeAsV1_WithContext_WritesContext()
    {
        var c = new ArazzoCriterion { Condition = "true", Context = "$response.body" };
        var w = MakeWriter(out var sw);
        c.SerializeAsV1(w);
        var json = JsonNode.Parse(sw.ToString())!.AsObject();
        Assert.Equal("$response.body", json["context"]!.GetValue<string>());
    }
}