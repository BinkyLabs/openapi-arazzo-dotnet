using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoWorkflowTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "getUserWorkflow",
            Summary = "Get user by ID",
            Description = "Retrieve a user by ID through the workflow.",
            Inputs = new ArazzoInput { Type = JsonSchemaType.Object },
            DependsOn = new HashSet<string> { "authWorkflow", "setupWorkflow" },
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep
                {
                    StepId = "step1",
                    OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1users~1{id}/get"
                }
            },
            SuccessActions = new List<IArazzoSuccessAction>
            {
                new ArazzoSuccessAction
                {
                    Name = "success",
                    Type = ArazzoSuccessType.End
                }
            },
            FailureActions = new List<IArazzoFailureAction>
            {
                new ArazzoFailureAction
                {
                    Name = "failure",
                    Type = ArazzoFailureType.End
                }
            },
            Outputs = new Dictionary<string, string>
            {
                ["user"] = "$response.body#/user"
            },
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter
                {
                    Name = "userId",
                    In = ParameterLocation.Path,
                    Value = "123"
                }
            },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-custom"] = new JsonNodeExtension(JsonNode.Parse("\"workflow-extension\"")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "workflowId": "getUserWorkflow",
            "summary": "Get user by ID",
            "description": "Retrieve a user by ID through the workflow.",
            "inputs": {
                "type": "object"
            },
            "dependsOn": [
                "authWorkflow",
                "setupWorkflow"
            ],
            "steps": [
                {
                    "stepId": "step1",
                    "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users~1{id}/get"
                }
            ],
            "successActions": [
                {
                    "name": "success",
                    "type": "end"
                }
            ],
            "failureActions": [
                {
                    "name": "failure",
                    "type": "end"
                }
            ],
            "outputs": {
                "user": "$response.body#/user"
            },
            "parameters": [
                {
                    "name": "userId",
                    "in": "path",
                    "value": "123"
                }
            ],
            "x-custom": "workflow-extension"
        }
        """;

        workflow.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_MinimalWorkflow_ShouldWriteCorrectJson()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "minimalWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "workflowId": "minimalWorkflow",
            "steps": [
                {
                    "stepId": "step1",
                    "operationId": "getUser"
                }
            ]
        }
        """;

        workflow.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "workflowId": "testWorkflow",
            "summary": "Test workflow",
            "description": "Test workflow description",
            "inputs": {
                "type": "object"
            },
            "dependsOn": ["workflow1", "workflow2"]
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var workflow = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.Equal("testWorkflow", workflow.WorkflowId);
        Assert.Equal("Test workflow", workflow.Summary);
        Assert.Equal("Test workflow description", workflow.Description);
        Assert.NotNull(workflow.Inputs);
        Assert.Equal(JsonSchemaType.Object, workflow.Inputs!.Type);

        Assert.NotNull(workflow.DependsOn);
        Assert.Contains("workflow1", workflow.DependsOn);
        Assert.Contains("workflow2", workflow.DependsOn);
    }

    [Fact]
    public void Deserialize_WithParameterList_LoadsParameters()
    {
        var json = """
        {
            "workflowId": "parameterListWorkflow",
            "parameters": [
                {
                    "name": "workflowLevelParamOne",
                    "in": "cookie",
                    "value": "someValue"
                },
                {
                    "name": "workflowLevelParamTwo",
                    "in": "header",
                    "value": "anotherValue"
                }
            ]
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var workflow = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.NotNull(workflow.Parameters);
        Assert.Equal(2, workflow.Parameters!.Count);
        Assert.Equal("workflowLevelParamOne", workflow.Parameters[0].Name);
        Assert.Equal(ParameterLocation.Cookie, workflow.Parameters[0].In);
        Assert.Equal("someValue", workflow.Parameters[0].Value?.GetValue<string>());
        Assert.Equal("workflowLevelParamTwo", workflow.Parameters[1].Name);
        Assert.Equal(ParameterLocation.Header, workflow.Parameters[1].In);
        Assert.Equal("anotherValue", workflow.Parameters[1].Value?.GetValue<string>());
    }

    [Fact]
    public void SerializeAsV1_ShouldHandleNullOptionalCollections()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "emptyWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "workflowId": "emptyWorkflow",
            "steps": [
                {
                    "stepId": "step1",
                    "operationId": "getUser"
                }
            ]
        }
        """;

        workflow.SerializeAsV1(writer);
        var result = textWriter.ToString();
        var jsonResultObject = JsonNode.Parse(result);
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithInvalidOutputKey_ThrowsArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "invalidOutputWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            },
            Outputs = new Dictionary<string, string>
            {
                ["invalid key"] = "$response.body#/id"
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains("Invalid key: 'invalid key'", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithInvalidOutputExpression_ThrowsArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "invalidOutputWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            },
            Outputs = new Dictionary<string, string>
            {
                ["userId"] = "response.body#/id"
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains("Values in ArazzoWorkflow.Outputs must be valid runtime expressions", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithNullSteps_ShouldThrowArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "missingStepsWorkflow"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Equal("Steps is required and must contain at least one element for ArazzoWorkflow serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithEmptySteps_ShouldThrowArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "emptyStepsWorkflow",
            Steps = new List<ArazzoStep>()
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Equal("Steps is required and must contain at least one element for ArazzoWorkflow serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithDuplicateStepIds_ShouldThrowArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "duplicateStepWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" },
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains("duplicate stepId 'step1'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithDuplicateParameterNameAndIn_ShouldThrowArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "duplicateParameterWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", WorkflowId = "childWorkflow" }
            },
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "input", Value = "1" },
                new ArazzoParameter { Name = "input", Value = "2" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains("duplicate parameter 'input' in '<unspecified>'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithSameParameterNameDifferentIn_ShouldSerialize()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "parameterWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            },
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "1" },
                new ArazzoParameter { Name = "id", In = ParameterLocation.Header, Value = "2" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        workflow.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!;

        Assert.Equal(2, json["parameters"]!.AsArray().Count);
    }

    [Fact]
    public void SerializeAsV1_WithOperationStepAndParameterWithoutIn_ShouldThrowArazzoSerializationException()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "operationParameterWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", OperationId = "getUser" }
            },
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "id", Value = "1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains("must specify 'in' when applied to an operation step", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowStepAndParameterWithoutIn_ShouldSerialize()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "workflowParameterWorkflow",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep { StepId = "step1", WorkflowId = "childWorkflow" }
            },
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "input", Value = "1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        workflow.SerializeAsV1(writer);
        var parameter = JsonNode.Parse(textWriter.ToString())!["parameters"]![0]!.AsObject();

        Assert.False(parameter.ContainsKey("in"));
    }

    [Fact]
    public void Deserialize_WithInvalidOutputKey_AddsDiagnosticError()
    {
        var json = """
        {
            "workflowId": "invalidOutputWorkflow",
            "outputs": {
                "invalid key": "$response.body#/id"
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var workflow = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.NotNull(workflow.Outputs);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("Invalid key: 'invalid key'", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithInvalidOutputExpression_AddsDiagnosticError()
    {
        var json = """
        {
            "workflowId": "invalidOutputWorkflow",
            "outputs": {
                "userId": "response.body#/id"
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var workflow = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.NotNull(workflow.Outputs);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("Values in ArazzoWorkflow.Outputs must be valid runtime expressions", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithReferences_LoadsReferenceTypes()
    {
        var json = """
        {
            "workflowId": "referenceWorkflow",
            "successActions": [
                {
                    "reference": "$components.successActions.successAction"
                }
            ],
            "failureActions": [
                {
                    "reference": "$components.failureActions.failureAction"
                }
            ],
            "parameters": [
                {
                    "reference": "$components.parameters.userId",
                    "value": "7"
                }
            ]
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var workflow = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.IsType<ArazzoSuccessActionReference>(Assert.Single(workflow.SuccessActions!));
        Assert.IsType<ArazzoFailureActionReference>(Assert.Single(workflow.FailureActions!));
        var parameter = Assert.IsType<ArazzoParameterReference>(Assert.Single(workflow.Parameters!));
        Assert.Equal("7", parameter.Value?.GetValue<string>());
    }


    [Theory]
    [InlineData(true, "successActions")]
    [InlineData(false, "failureActions")]
    public void SerializeAsV1_WithDuplicateActionNames_ShouldThrowArazzoSerializationException(bool useSuccessActions, string propertyName)
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "duplicateActionWorkflow",
            Steps = [new ArazzoStep { StepId = "step1", OperationId = "getUser" }]
        };
        if (useSuccessActions)
        {
            workflow.SuccessActions =
            [
                new ArazzoSuccessAction { Name = "duplicateAction", Type = ArazzoSuccessType.End },
                new ArazzoSuccessAction { Name = "duplicateAction", Type = ArazzoSuccessType.End }
            ];
        }
        else
        {
            workflow.FailureActions =
            [
                new ArazzoFailureAction { Name = "duplicateAction", Type = ArazzoFailureType.End },
                new ArazzoFailureAction { Name = "duplicateAction", Type = ArazzoFailureType.End }
            ];
        }
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains($"{propertyName} contains duplicate action 'duplicateAction'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, "successActions", "$components.successActions.reusableAction")]
    [InlineData(false, "failureActions", "$components.failureActions.reusableAction")]
    public void SerializeAsV1_WithDuplicateActionReferences_ShouldThrowArazzoSerializationException(bool useSuccessActions, string propertyName, string reference)
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "duplicateActionReferenceWorkflow",
            Steps = [new ArazzoStep { StepId = "step1", OperationId = "getUser" }]
        };
        if (useSuccessActions)
        {
            workflow.SuccessActions =
            [
                new ArazzoSuccessActionReference("reusableAction"),
                new ArazzoSuccessActionReference("reusableAction")
            ];
        }
        else
        {
            workflow.FailureActions =
            [
                new ArazzoFailureActionReference("reusableAction"),
                new ArazzoFailureActionReference("reusableAction")
            ];
        }
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1(writer));

        Assert.Contains($"{propertyName} contains duplicate action '{reference}'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("successActions")]
    [InlineData("failureActions")]
    public void Deserialize_WithDuplicateActionNames_AddsDiagnosticError(string propertyName)
    {
        var json = $$"""
        {
            "workflowId": "duplicateActionWorkflow",
            "{{propertyName}}": [
                {
                    "name": "duplicateAction",
                    "type": "end"
                },
                {
                    "name": "duplicateAction",
                    "type": "end"
                }
            ]
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains($"{propertyName} contains duplicate action 'duplicateAction'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("successActions", "$components.successActions.reusableAction")]
    [InlineData("failureActions", "$components.failureActions.reusableAction")]
    public void Deserialize_WithDuplicateActionReferences_AddsDiagnosticError(string propertyName, string reference)
    {
        var json = $$"""
        {
            "workflowId": "duplicateActionReferenceWorkflow",
            "{{propertyName}}": [
                {
                    "reference": "{{reference}}"
                },
                {
                    "reference": "{{reference}}"
                }
            ]
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains($"{propertyName} contains duplicate action '{reference}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SerializeAsV1_WithDistinctActionNames_ShouldNotThrow(bool useSuccessActions)
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "distinctActionWorkflow",
            Steps = [new ArazzoStep { StepId = "step1", OperationId = "getUser" }]
        };
        if (useSuccessActions)
        {
            workflow.SuccessActions =
            [
                new ArazzoSuccessAction { Name = "firstAction", Type = ArazzoSuccessType.End },
                new ArazzoSuccessAction { Name = "secondAction", Type = ArazzoSuccessType.End }
            ];
        }
        else
        {
            workflow.FailureActions =
            [
                new ArazzoFailureAction { Name = "firstAction", Type = ArazzoFailureType.End },
                new ArazzoFailureAction { Name = "secondAction", Type = ArazzoFailureType.End }
            ];
        }
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        workflow.SerializeAsV1(writer);
    }

    [Theory]
    [InlineData("successActions")]
    [InlineData("failureActions")]
    public void Deserialize_WithDistinctActionNames_DoesNotAddDiagnosticError(string propertyName)
    {
        var json = $$"""
        {
            "workflowId": "distinctActionWorkflow",
            "{{propertyName}}": [
                {
                    "name": "firstAction",
                    "type": "end"
                },
                {
                    "name": "secondAction",
                    "type": "end"
                }
            ]
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.DoesNotContain(parsingContext.Diagnostic.Errors, error => error.Message.Contains("contains duplicate action", StringComparison.Ordinal));
    }

}