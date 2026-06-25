using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoStepTests
{
    [Fact]
    public void BuildOperationPointer_ShouldEscapePathAndNormalizeMethod()
    {
        var pointer = ArazzoStep.BuildOperationPointer("/pets/{petId}", "GET");

        Assert.Equal("#/paths/~1pets~1{petId}/get", pointer);
    }

    [Fact]
    public void BuildOperationPointer_WithEmptyStrings_ShouldThrowArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => ArazzoStep.BuildOperationPointer(string.Empty, "get"));
        Assert.ThrowsAny<ArgumentException>(() => ArazzoStep.BuildOperationPointer("/pets", string.Empty));
    }

    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        var step = new ArazzoStep
        {
            Description = "Create a new user",
            StepId = "createUser",
            OperationId = "createUserOp",
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Value = "42"
                }
            },
            RequestBody = new ArazzoRequestBody
            {
                ContentType = "application/json",
                Payload = JsonNode.Parse("{\"name\":\"John\"}")!
            },
            SuccessCriteria = new List<ArazzoCriterion>
            {
                new ArazzoCriterion
                {
                    Context = "$statusCode",
                    Condition = "200"
                }
            },
            OnSuccess = new List<IArazzoSuccessAction>
            {
                new ArazzoSuccessAction
                {
                    Name = "nextStep",
                    Type = ArazzoSuccessType.Goto,
                    StepId = "step2"
                }
            },
            OnFailure = new List<IArazzoFailureAction>
            {
                new ArazzoFailureAction
                {
                    Name = "retry",
                    Type = ArazzoFailureType.Retry,
                    RetryAfter = 1.5m,
                    RetryLimit = 3
                }
            },
            Outputs = new Dictionary<string, string>
            {
                ["userId"] = "$response.body#/id"
            },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-internal"] = new JsonNodeExtension(JsonNode.Parse("true")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "description": "Create a new user",
            "stepId": "createUser",
            "operationId": "createUserOp",
            "parameters": [
                {
                    "name": "id",
                    "in": "path",
                    "value": "42"
                }
            ],
            "requestBody": {
                "contentType": "application/json",
                "payload": {
                    "name": "John"
                }
            },
            "successCriteria": [
                {
                    "context": "$statusCode",
                    "condition": "200"
                }
            ],
            "onSuccess": [
                {
                    "name": "nextStep",
                    "type": "goto",
                    "stepId": "step2"
                }
            ],
            "onFailure": [
                {
                    "name": "retry",
                    "type": "retry",
                    "retryAfter": 1.5,
                    "retryLimit": 3
                }
            ],
            "outputs": {
                "userId": "$response.body#/id"
            },
            "x-internal": true
        }
        """;

        step.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_MinimalStep_ShouldWriteCorrectJson()
    {
        var step = new ArazzoStep
        {
            StepId = "step1",
            OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1users~1{id}/get"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "stepId": "step1",
            "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users~1{id}/get"
        }
        """;

        step.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithPlainOperationPath_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "plainOperationPathStep",
            OperationPath = "/users/{id}"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("must reference a sourceDescription URL runtime expression followed by a JSON Pointer to an operation path", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithQueryOperationPath_ShouldWriteCorrectJson()
    {
        var step = new ArazzoStep
        {
            StepId = "queryOperationPathStep",
            OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1somePath/query"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        step.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("{$sourceDescriptions.source1.url}#/paths/~1somePath/query", jsonResultObject?[ArazzoConstants.ArazzoStepOperationPath]?.GetValue<string>());
    }

    [Fact]
    public void SerializeAsV1_WithInvalidOperationOption_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "invalidOperationOptionStep",
            OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1somePath/invalid"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("must reference a sourceDescription URL runtime expression followed by a JSON Pointer to an operation path", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_WithPlainOperationPath_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "plainOperationPathStep",
            "operationPath": "/users/{id}"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("must reference a sourceDescription URL runtime expression followed by a JSON Pointer to an operation path", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithQueryOperationPath_ShouldNotAddDiagnosticError()
    {
        var json = """
        {
            "stepId": "queryOperationPathStep",
            "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1somePath/query"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.DoesNotContain(parsingContext.Diagnostic.Errors, error => error.Message.Contains("operationPath", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "description": "Fetch user details",
            "stepId": "getUser",
            "workflowId": "userWorkflow",
            "parameters": [
                {
                    "name": "userId",
                    "in": "path",
                    "value": "123"
                }
            ],
            "successCriteria": [
                {
                    "context": "$statusCode",
                    "condition": "200"
                }
            ],
            "outputs": {
                "userName": "$response.body#/name",
                "userEmail": "$response.body#/email"
            },
            "x-custom": "metadata"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var step = ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Equal("Fetch user details", step.Description);
        Assert.Equal("getUser", step.StepId);
        Assert.Equal("userWorkflow", step.WorkflowId);
        Assert.NotNull(step.Parameters);
        Assert.Single(step.Parameters!);
        Assert.Equal("userId", step.Parameters[0].Name);
        Assert.NotNull(step.SuccessCriteria);
        Assert.Single(step.SuccessCriteria!);
        Assert.Equal("$statusCode", step.SuccessCriteria[0].Context);
        Assert.NotNull(step.Outputs);
        Assert.Equal(2, step.Outputs!.Count);
        Assert.Equal("$response.body#/name", step.Outputs["userName"]);
        Assert.Equal("$response.body#/email", step.Outputs["userEmail"]);
        Assert.NotNull(step.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(step.Extensions!["x-custom"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"metadata\""), extension.Node));
    }

    [Fact]
    public void SerializeAsV1_WithInvalidOutputKey_ThrowsArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "invalidOutputStep",
            OperationId = "getUser",
            Outputs = new Dictionary<string, string>
            {
                ["invalid key"] = "$response.body#/id"
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("Invalid key: 'invalid key'", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithMultipleOperationReferences_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "conflictingStep",
            OperationId = "getUser",
            OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1users/get"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("can define only one of operationId, operationPath, or workflowId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowIdAndOperationId_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "conflictingStep",
            WorkflowId = "childWorkflow",
            OperationId = "getUser"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("can define only one of operationId, operationPath, or workflowId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowIdAndOperationPath_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "conflictingStep",
            WorkflowId = "childWorkflow",
            OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1users/get"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("can define only one of operationId, operationPath, or workflowId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithoutTarget_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "untargetedStep"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("must define exactly one of operationId, operationPath, or workflowId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowTargetAndRequestBody_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "workflowRequestStep",
            WorkflowId = "childWorkflow",
            RequestBody = new ArazzoRequestBody()
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("requestBody can only be specified when the step targets operationId or operationPath", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithDuplicateParameterNameAndIn_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "duplicateParameterStep",
            WorkflowId = "workflowTarget",
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "1" },
                new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "2" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("duplicate parameter 'id' in 'query'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithSameParameterNameDifferentIn_ShouldSerialize()
    {
        var step = new ArazzoStep
        {
            StepId = "parameterStep",
            OperationId = "getUser",
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "1" },
                new ArazzoParameter { Name = "id", In = ParameterLocation.Header, Value = "2" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        step.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!;

        Assert.Equal(2, json["parameters"]!.AsArray().Count);
    }

    [Fact]
    public void SerializeAsV1_WithOperationTargetAndParameterWithoutIn_ShouldThrowArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "operationStep",
            OperationId = "getUser",
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "id", Value = "1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("must specify 'in' when the step targets an operation", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowTargetAndParameterWithoutIn_ShouldSerialize()
    {
        var step = new ArazzoStep
        {
            StepId = "workflowStep",
            WorkflowId = "childWorkflow",
            Parameters = new List<IArazzoParameter>
            {
                new ArazzoParameter { Name = "input", Value = "1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        step.SerializeAsV1(writer);
        var parameter = JsonNode.Parse(textWriter.ToString())!["parameters"]![0]!.AsObject();

        Assert.False(parameter.ContainsKey("in"));
    }

    [Fact]
    public void Deserialize_WithInvalidOutputKey_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "invalidOutputStep",
            "outputs": {
                "invalid key": "$response.body#/id"
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var step = ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.NotNull(step.Outputs);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("Invalid key: 'invalid key'", StringComparison.Ordinal));
    }

    [Fact]
    public void SerializeAsV1_WithInvalidOutputValue_ThrowsArazzoSerializationException()
    {
        var step = new ArazzoStep
        {
            StepId = "invalidOutputValueStep",
            OperationId = "getUser",
            Outputs = new Dictionary<string, string>
            {
                ["userId"] = "not-a-runtime-expression"
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains("Invalid value for key 'userId': 'not-a-runtime-expression'", exception.Message);
    }

    [Fact]
    public void Deserialize_WithInvalidOutputValue_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "invalidOutputValueStep",
            "outputs": {
                "userId": "not-a-runtime-expression"
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var step = ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.NotNull(step.Outputs);
        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("Invalid value for key 'userId': 'not-a-runtime-expression'", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithoutTarget_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "untargetedStep"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("must define exactly one of operationId, operationPath, or workflowId", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithWorkflowIdAndOperationId_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "conflictingStep",
            "workflowId": "childWorkflow",
            "operationId": "getUser"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("can define only one of operationId, operationPath, or workflowId", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithWorkflowIdAndOperationPath_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "conflictingStep",
            "workflowId": "childWorkflow",
            "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users/get"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("can define only one of operationId, operationPath, or workflowId", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithWorkflowTargetAndRequestBody_AddsDiagnosticError()
    {
        var json = """
        {
            "stepId": "workflowRequestStep",
            "workflowId": "childWorkflow",
            "requestBody": {}
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("requestBody can only be specified when the step targets operationId or operationPath", StringComparison.Ordinal));
    }

    [Fact]
    public void SerializeAsV1_WithReferences_WritesReferenceObjects()
    {
        var step = new ArazzoStep
        {
            StepId = "referenceStep",
            WorkflowId = "childWorkflow",
            Parameters = [new ArazzoParameterReference("userId") { Value = JsonValue.Create("7") }],
            OnSuccess = [new ArazzoSuccessActionReference("nextAction")],
            OnFailure = [new ArazzoFailureActionReference("retryAction")]
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        step.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.parameters.userId", json?["parameters"]?[0]?["reference"]?.GetValue<string>());
        Assert.Equal("7", json?["parameters"]?[0]?["value"]?.GetValue<string>());
        Assert.Equal("$components.successActions.nextAction", json?["onSuccess"]?[0]?["reference"]?.GetValue<string>());
        Assert.Equal("$components.failureActions.retryAction", json?["onFailure"]?[0]?["reference"]?.GetValue<string>());
    }


    [Theory]
    [InlineData(true, "onSuccess")]
    [InlineData(false, "onFailure")]
    public void SerializeAsV1_WithDuplicateActionNames_ShouldThrowArazzoSerializationException(bool useSuccessActions, string propertyName)
    {
        var step = new ArazzoStep
        {
            StepId = "duplicateActionStep",
            OperationId = "getUser"
        };
        if (useSuccessActions)
        {
            step.OnSuccess =
            [
                new ArazzoSuccessAction { Name = "duplicateAction", Type = ArazzoSuccessType.End },
                new ArazzoSuccessAction { Name = "duplicateAction", Type = ArazzoSuccessType.End }
            ];
        }
        else
        {
            step.OnFailure =
            [
                new ArazzoFailureAction { Name = "duplicateAction", Type = ArazzoFailureType.End },
                new ArazzoFailureAction { Name = "duplicateAction", Type = ArazzoFailureType.End }
            ];
        }
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains($"{propertyName} contains duplicate action 'duplicateAction'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, "onSuccess", "$components.successActions.reusableAction")]
    [InlineData(false, "onFailure", "$components.failureActions.reusableAction")]
    public void SerializeAsV1_WithDuplicateActionReferences_ShouldThrowArazzoSerializationException(bool useSuccessActions, string propertyName, string reference)
    {
        var step = new ArazzoStep
        {
            StepId = "duplicateActionReferenceStep",
            OperationId = "getUser"
        };
        if (useSuccessActions)
        {
            step.OnSuccess =
            [
                new ArazzoSuccessActionReference("reusableAction"),
                new ArazzoSuccessActionReference("reusableAction")
            ];
        }
        else
        {
            step.OnFailure =
            [
                new ArazzoFailureActionReference("reusableAction"),
                new ArazzoFailureActionReference("reusableAction")
            ];
        }
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => step.SerializeAsV1(writer));

        Assert.Contains($"{propertyName} contains duplicate action '{reference}'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("onSuccess")]
    [InlineData("onFailure")]
    public void Deserialize_WithDuplicateActionNames_AddsDiagnosticError(string propertyName)
    {
        var json = $$"""
        {
            "stepId": "duplicateActionStep",
            "operationId": "getUser",
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

        _ = ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains($"{propertyName} contains duplicate action 'duplicateAction'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("onSuccess", "$components.successActions.reusableAction")]
    [InlineData("onFailure", "$components.failureActions.reusableAction")]
    public void Deserialize_WithDuplicateActionReferences_AddsDiagnosticError(string propertyName, string reference)
    {
        var json = $$"""
        {
            "stepId": "duplicateActionReferenceStep",
            "operationId": "getUser",
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

        _ = ArazzoV1Deserializer.LoadStep(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains($"{propertyName} contains duplicate action '{reference}'", StringComparison.Ordinal));
    }

}