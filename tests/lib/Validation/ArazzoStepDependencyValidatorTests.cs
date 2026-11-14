using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Validation;

public class ArazzoStepDependencyValidatorTests
{
    [Theory]
    [InlineData("missing", "unknown stepId 'missing'")]
    [InlineData("First", "unknown stepId 'First'")]
    [InlineData("$workflows.missing.steps.first", "unknown workflowId 'missing'")]
    [InlineData("$workflows.other.steps.missing", "unknown stepId 'missing'")]
    [InlineData("$steps.first", "must reference a stepId")]
    [InlineData("$sourceDescriptions.missing.other.steps.first", "unknown sourceDescription 'missing'")]
    [InlineData("$sourceDescriptions.api.other.steps.first", "type must be 'arazzo'")]
    public void UnknownOrInvalidExplicitDependency_IsRejected(string dependency, string expectedMessage)
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![1].DependsOn = new HashSet<string> { dependency };

        AssertRejected(document, expectedMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SequentialForwardOutputReference_IsRejectedByReaderAndWriter(bool legacy)
    {
        var version = legacy ? "1.0.1" : "1.1.0";
        var jsonNode = JsonNode.Parse($$$"""
        {
            "arazzo": "{{{version}}}",
            "info": {"title": "T", "version": "1"},
            "sourceDescriptions": [{"name": "api", "url": "https://example.com/api", "type": "openapi"}],
            "workflows": [{
                "workflowId": "wf",
                "steps": [
                    {"stepId": "first", "operationId": "getUser", "parameters": [{"name": "id", "in": "query", "value": "$steps.second.outputs.id"}]},
                    {"stepId": "second", "operationId": "getUser", "outputs": {"id": "$response.body#/id"}}
                ]
            }]
        }
        """);
        Assert.NotNull(jsonNode);
        var context = new ParsingContext(new());

        var document = context.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("unsatisfiable forward reference", StringComparison.Ordinal));
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        var exception = Assert.Throws<ArazzoSerializationException>(() =>
        {
            if (legacy)
                document.SerializeAsV1(writer);
            else
                document.SerializeAsV1_1(writer);
        });
        Assert.Contains("unsatisfiable forward reference", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("parameter")]
    [InlineData("payload")]
    [InlineData("replacement")]
    [InlineData("selector")]
    [InlineData("criterion")]
    [InlineData("output")]
    [InlineData("correlation")]
    [InlineData("action")]
    public void OutputReferencesAcrossExpressionLocations_AreImplicitDependencies(string location)
    {
        var document = CreateDocument();
        var first = document.Workflows![0].Steps![0];
        const string expression = "$steps.second.outputs.id";
        switch (location)
        {
            case "parameter":
                first.Parameters = [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = new ArazzoRuntimeExpression(expression) }];
                break;
            case "payload":
                first.RequestBody = new ArazzoRequestBody { Payload = JsonNode.Parse("""{"items": [{"id": "prefix-{$steps.second.outputs.id}"}]}""") };
                break;
            case "replacement":
                first.RequestBody = new ArazzoRequestBody
                {
                    Payload = JsonNode.Parse("""{"id": 0}"""),
                    Replacements = [new ArazzoPayloadReplacement { Target = "/id", Value = new ArazzoRuntimeExpression(expression) }]
                };
                break;
            case "selector":
                first.Parameters =
                [
                    new ArazzoParameter
                    {
                        Name = "id",
                        In = ParameterLocation.Query,
                        Value = new ArazzoSelector { Context = new(expression), Selector = "$.id", Type = ArazzoPayloadRequirementTargetSelectorType.JsonPath }
                    }
                ];
                break;
            case "criterion":
                first.SuccessCriteria = [new ArazzoCriterion { Condition = $"{expression} == 'expected'" }];
                break;
            case "output":
                first.Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = expression };
                break;
            case "correlation":
                Assert.NotNull(document.SourceDescriptions);
                document.SourceDescriptions[0].Type = ArazzoDescriptionType.AsyncAPI;
                foreach (var workflow in document.Workflows)
                {
                    Assert.NotNull(workflow.Steps);
                    foreach (var step in workflow.Steps)
                        step.Action = ArazzoStepAction.Receive;
                }
                first.CorrelationId = expression;
                break;
            case "action":
                first.OnSuccess =
                [
                    new ArazzoSuccessAction
                    {
                        Name = "goto",
                        Type = ArazzoSuccessType.Goto,
                        WorkflowId = "other",
                        Parameters = [new ArazzoParameter { Name = "id", Value = new ArazzoRuntimeExpression(expression) }]
                    }
                ];
                break;
        }

        AssertRejected(document, "unsatisfiable forward reference");
    }

    [Fact]
    public void UnknownImplicitOutputReference_IsRejected()
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![1].Parameters =
            [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.missing.outputs.id" }];

        AssertRejected(document, "unknown stepId 'missing'");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReferencedParameterValues_RespectValueOverrides(bool overrideValue)
    {
        var document = CreateDocument();
        document.Components = new ArazzoComponent
        {
            Parameters = new Dictionary<string, ArazzoParameter>
            {
                ["id"] = new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.second.outputs.id" }
            }
        };
        var reference = new ArazzoParameterReference("id", document);
        if (overrideValue)
            reference.Value = "override";
        document.Workflows![0].Steps![0].Parameters = [reference];

        if (overrideValue)
            AssertAccepted(document);
        else
            AssertRejected(document, "unsatisfiable forward reference");
    }

    [Fact]
    public void InheritedParameters_CreateImplicitDependencies()
    {
        var document = CreateDocument();
        document.Workflows![0].Parameters =
            [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.second.outputs.id" }];

        AssertRejected(document, "unsatisfiable forward reference");
    }

    [Fact]
    public void InheritedReusableActions_CreateImplicitDependencies()
    {
        var document = CreateDocument();
        document.Components = new ArazzoComponent
        {
            FailureActions = new Dictionary<string, ArazzoFailureAction>
            {
                ["retry"] = new ArazzoFailureAction
                {
                    Name = "retry",
                    Type = ArazzoFailureType.Retry,
                    WorkflowId = "other",
                    Criteria = [new ArazzoCriterion { Condition = "$steps.second.outputs.id != ''" }]
                }
            }
        };
        document.Workflows![0].FailureActions = [new ArazzoFailureActionReference("retry", document)];

        AssertRejected(document, "unsatisfiable forward reference");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitOrMixedDependencyCycle_IsRejected(bool implicitDependency)
    {
        var document = CreateDocument();
        var steps = document.Workflows![0].Steps!;
        steps[0].DependsOn = new HashSet<string> { "second" };
        if (implicitDependency)
            steps[1].Parameters = [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.first.outputs.id" }];
        else
            steps[1].DependsOn = new HashSet<string> { "first" };

        AssertRejected(document, "dependency cycle");
    }

    [Fact]
    public void ImplicitOnlyDependencyCycle_ReportsBothCycleAndUnsatisfiableOrdering()
    {
        var document = CreateDocument();
        var steps = document.Workflows![0].Steps!;
        steps[0].Parameters = [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.second.outputs.id" }];
        steps[1].Parameters = [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.first.outputs.id" }];

        Assert.Contains(ArazzoStepDependencyValidator.Validate(document), error => error.Contains("dependency cycle", StringComparison.Ordinal));
        AssertRejected(document, "unsatisfiable forward reference");
    }

    [Fact]
    public void CrossWorkflowDependencyCycle_IsRejected()
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![0].DependsOn = new HashSet<string> { "$workflows.other.steps.first" };
        document.Workflows[1].Steps![0].DependsOn = new HashSet<string> { "$workflows.wf.steps.first" };

        AssertRejected(document, "dependency cycle");
    }

    [Fact]
    public void StepDependencyConflictingWithWorkflowPrerequisite_IsRejected()
    {
        var document = CreateDocument();
        document.Workflows![0].DependsOn = new HashSet<string> { "other" };
        document.Workflows[1].Steps![0].DependsOn = new HashSet<string> { "$workflows.wf.steps.first" };

        AssertRejected(document, "dependency cycle");
    }

    [Fact]
    public void WorkflowOutputReferences_WaitForWorkflowCompletion()
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![0].Parameters =
            [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$workflows.other.outputs.id" }];
        document.Workflows[1].Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = "$steps.first.outputs.id" };
        document.Workflows[1].Steps![0].DependsOn = new HashSet<string> { "$workflows.wf.steps.first" };

        AssertRejected(document, "dependency cycle");
    }

    [Fact]
    public void SelfDependencyBeforeStepExecution_IsRejected()
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![0].Parameters =
            [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.first.outputs.id" }];

        AssertRejected(document, "cannot depend on itself");
    }

    [Fact]
    public void ExplicitOrderingAllowsAcyclicForwardOutputReferences()
    {
        var document = CreateDocument();
        var steps = document.Workflows![0].Steps!;
        steps[0].DependsOn = new HashSet<string> { "second", "$workflows.other.steps.first", "$sourceDescriptions.external.remote.steps.ready" };
        steps[0].Parameters = [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.second.outputs.id" }];

        AssertAccepted(document);
    }

    [Fact]
    public void CurrentStepOutputsInPostExecutionActions_DoNotCreateSelfDependencies()
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![0].OnSuccess =
        [
            new ArazzoSuccessAction
            {
                Name = "goto",
                Type = ArazzoSuccessType.Goto,
                WorkflowId = "other",
                Criteria = [new ArazzoCriterion { Condition = "$steps.first.outputs.id != ''" }],
                Parameters = [new ArazzoParameter { Name = "id", Value = "$steps.first.outputs.id" }]
            }
        ];

        AssertAccepted(document);
    }

    [Fact]
    public void SequentialBackwardOutputReferences_AreAccepted()
    {
        var document = CreateDocument();
        document.Workflows![0].Steps![1].Parameters =
            [new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "$steps.first.outputs.id" }];

        AssertAccepted(document);
    }

    [Fact]
    public void OverriddenInheritedParametersAndLiteralReferences_DoNotCreateDependencies()
    {
        var document = CreateDocument();
        var workflow = document.Workflows![0];
        workflow.Parameters = [new ArazzoParameter { Name = "X-Id", In = ParameterLocation.Header, Value = "$steps.second.outputs.id" }];
        workflow.Steps![0].Parameters = [new ArazzoParameter { Name = "x-id", In = ParameterLocation.Header, Value = "override" }];
        workflow.Steps[1].Parameters = [new ArazzoParameter { Name = "X-Id", In = ParameterLocation.Header, Value = "override" }];
        workflow.Steps[0].Description = "$steps.missing.outputs.id";
        workflow.Steps[0].SuccessCriteria = [new ArazzoCriterion { Condition = """$statusCode == 200 && '$steps.missing.outputs.id' != ''""" }];
        workflow.Steps[0].RequestBody = new ArazzoRequestBody { Payload = "literal $steps.missing.outputs.id" };

        AssertAccepted(document);
    }

    private static void AssertRejected(ArazzoDocument document, string expectedMessage)
    {
        var context = new ParsingContext(new());
        ArazzoSemanticReferenceValidator.ValidateDeserialization(document, context);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
        using var textWriter = new StringWriter();
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1_1(new OpenApiJsonWriter(textWriter)));
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    private static void AssertAccepted(ArazzoDocument document)
    {
        var context = new ParsingContext(new());
        ArazzoSemanticReferenceValidator.ValidateDeserialization(document, context);
        Assert.Empty(context.Diagnostic.Errors);
        using var textWriter = new StringWriter();
        document.SerializeAsV1_1(new OpenApiJsonWriter(textWriter));
    }

    private static ArazzoDocument CreateDocument() => new()
    {
        Info = new ArazzoInfo { Title = "T", Version = "1" },
        SourceDescriptions =
        [
            new ArazzoSourceDescription { Name = "api", Type = ArazzoDescriptionType.OpenAPI, Url = new Uri("https://example.com/api") },
            new ArazzoSourceDescription { Name = "external", Type = ArazzoDescriptionType.Arazzo, Url = new Uri("https://example.com/external.arazzo") }
        ],
        Workflows =
        [
            new ArazzoWorkflow
            {
                WorkflowId = "wf",
                Steps =
                [
                    new ArazzoStep { StepId = "first", OperationId = "getUser", Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = "$response.body#/id" } },
                    new ArazzoStep { StepId = "second", OperationId = "getUser", Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = "$response.body#/id" } }
                ]
            },
            new ArazzoWorkflow
            {
                WorkflowId = "other",
                Steps = [new ArazzoStep { StepId = "first", OperationId = "getUser", Outputs = new Dictionary<string, ArazzoOutputValueUnion> { ["id"] = "$response.body#/id" } }]
            }
        ]
    };
}