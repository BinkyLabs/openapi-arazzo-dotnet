using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoFailureActionTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson_WithAllProperties()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "failureAction1",
            Type = ArazzoFailureType.Retry,
            WorkflowId = "workflow123",
            RetryAfter = 5.5m,
            RetryLimit = 3ul,
            Criteria = new List<ArazzoCriterion>
            {
                new ArazzoCriterion
                {
                    Context = "$statusCode",
                    Condition = "500"
                }
            },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonNode.Parse("{\"note\":\"test\"}")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "name": "failureAction1",
            "type": "retry",
            "workflowId": "workflow123",
            "retryAfter": 5.5,
            "retryLimit": 3,
            "criteria": [
                {
                    "context": "$statusCode",
                    "condition": "500"
                }
            ],
            "x-extra": {
                "note": "test"
            }
        }
        """;

        failureAction.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson_WithRequiredPropertiesOnly()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "endAction",
            Type = ArazzoFailureType.End
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "name": "endAction",
            "type": "end"
        }
        """;

        failureAction.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson_WithGotoType()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "gotoAction",
            Type = ArazzoFailureType.Goto,
            StepId = "step789"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "name": "gotoAction",
            "type": "goto",
            "stepId": "step789"
        }
        """;

        failureAction.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_WithRetryAndNoRetryLimit_ShouldOmitRetryLimit()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "retryAction",
            Type = ArazzoFailureType.Retry
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        failureAction.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!;

        Assert.Null(json["retryLimit"]);
    }

    [Fact]
    public void SerializeAsV1_WithRetryAfterOnNonRetryType_ShouldThrowArazzoSerializationException()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "endAction",
            Type = ArazzoFailureType.End,
            RetryAfter = 1
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => failureAction.SerializeAsV1(writer));

        Assert.Contains("retryAfter can only be specified when type is retry", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithRetryLimitOnNonRetryType_ShouldThrowArazzoSerializationException()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "endAction",
            Type = ArazzoFailureType.End,
            RetryLimit = 1
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => failureAction.SerializeAsV1(writer));

        Assert.Contains("retryLimit can only be specified when type is retry", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithNegativeRetryAfter_ShouldThrowArazzoSerializationException()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "retryAction",
            Type = ArazzoFailureType.Retry,
            RetryAfter = -1
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => failureAction.SerializeAsV1(writer));

        Assert.Contains("retryAfter must be a non-negative decimal", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_ShouldThrowException_WhenNameIsNull()
    {
        var failureAction = new ArazzoFailureAction
        {
            Type = ArazzoFailureType.End
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => failureAction.SerializeAsV1(writer));
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowIdAndStepId_ShouldThrowArazzoSerializationException()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "retryAction",
            Type = ArazzoFailureType.Retry,
            WorkflowId = "workflow1",
            StepId = "step1"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => failureAction.SerializeAsV1(writer));

        Assert.Contains("can define only one of workflowId or stepId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithEndAndTargetField_ShouldThrowArazzoSerializationException()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "endAction",
            Type = ArazzoFailureType.End,
            StepId = "step1"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => failureAction.SerializeAsV1(writer));

        Assert.Contains("type=end must not define workflowId or stepId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithGotoAndNoTargetField_ShouldThrowArazzoSerializationException()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "gotoAction",
            Type = ArazzoFailureType.Goto
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => failureAction.SerializeAsV1(writer));

        Assert.Contains("type=goto must define exactly one of workflowId or stepId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_ShouldThrowException_WhenTypeIsNull()
    {
        var failureAction = new ArazzoFailureAction
        {
            Name = "testAction"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => failureAction.SerializeAsV1(writer));
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "name": "retryAction",
            "type": "retry",
            "workflowId": "mainWorkflow",
            "retryAfter": 10.5,
            "retryLimit": 5,
            "criteria": [
                {
                    "context": "$response.body#/error",
                    "condition": "true"
                }
            ],
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureAction>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal("retryAction", failureAction.Name);
        Assert.Equal(ArazzoFailureType.Retry, failureAction.Type);
        Assert.Equal("mainWorkflow", failureAction.WorkflowId);
        Assert.Null(failureAction.StepId);
        Assert.Equal(10.5m, failureAction.RetryAfter);
        Assert.Equal(5ul, failureAction.RetryLimit);
        Assert.NotNull(failureAction.Criteria);
        Assert.Single(failureAction.Criteria);
        Assert.Equal("$response.body#/error", failureAction.Criteria[0].Context);
        Assert.Equal("true", failureAction.Criteria[0].Condition);
        Assert.NotNull(failureAction.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(failureAction.Extensions!["x-flag"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("true"), extension.Node));
    }

    [Fact]
    public void Deserialize_ShouldSetRequiredPropertiesOnly()
    {
        var json = """
        {
            "name": "simpleEnd",
            "type": "end"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureAction>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal("simpleEnd", failureAction.Name);
        Assert.Equal(ArazzoFailureType.End, failureAction.Type);
        Assert.Null(failureAction.WorkflowId);
        Assert.Null(failureAction.StepId);
        Assert.Null(failureAction.RetryAfter);
        Assert.Equal(1ul, failureAction.RetryLimit);
        Assert.Null(failureAction.Criteria);
        Assert.Null(failureAction.Extensions);
    }

    [Fact]
    public void Deserialize_WithRetryAndNoRetryLimit_ShouldUseDefaultRetryLimit()
    {
        var json = """
        {
            "name": "retryAction",
            "type": "retry"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureAction>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal(1ul, failureAction.RetryLimit);
        Assert.Empty(parsingContext.Diagnostic.Errors);
    }

    [Fact]
    public void Deserialize_WithRetryAfterOnNonRetryType_AddsDiagnosticError()
    {
        var json = """
        {
            "name": "endAction",
            "type": "end",
            "retryAfter": 1
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("retryAfter can only be specified when type is retry", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithRetryLimitOnNonRetryType_AddsDiagnosticError()
    {
        var json = """
        {
            "name": "endAction",
            "type": "end",
            "retryLimit": 1
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("retryLimit can only be specified when type is retry", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithNegativeRetryAfter_AddsDiagnosticError()
    {
        var json = """
        {
            "name": "retryAction",
            "type": "retry",
            "retryAfter": -1
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("retryAfter must be a non-negative decimal", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithInvalidRetryAfter_AddsDiagnosticError()
    {
        var json = """
        {
            "name": "retryAction",
            "type": "retry",
            "retryAfter": "not-a-number"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("retryAfter must be a non-negative decimal", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("\"not-a-number\"")]
    [InlineData("-1")]
    [InlineData("1.5")]
    public void Deserialize_WithInvalidRetryLimit_AddsDiagnosticError(string retryLimit)
    {
        var json = $$"""
        {
            "name": "retryAction",
            "type": "retry",
            "retryLimit": {{retryLimit}}
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("retryLimit must be a non-negative integer", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "name": "endAction", "type": "end", "stepId": "step1" }""", "type=end must not define workflowId or stepId")]
    [InlineData("""{ "name": "gotoAction", "type": "goto" }""", "type=goto must define exactly one of workflowId or stepId")]
    [InlineData("""{ "name": "retryAction", "type": "retry", "workflowId": "workflow1", "stepId": "step1" }""", "can define only one of workflowId or stepId")]
    public void Deserialize_WithInvalidTypeDependentTargetFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_ShouldHandleGotoType()
    {
        var json = """
        {
            "name": "gotoFailure",
            "type": "goto",
            "stepId": "errorHandler"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureAction>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal("gotoFailure", failureAction.Name);
        Assert.Equal(ArazzoFailureType.Goto, failureAction.Type);
        Assert.Null(failureAction.WorkflowId);
        Assert.Equal("errorHandler", failureAction.StepId);
        Assert.Null(failureAction.RetryAfter);
        Assert.Equal(1ul, failureAction.RetryLimit);
        Assert.Null(failureAction.Criteria);
        Assert.Null(failureAction.Extensions);
    }

    [Fact]
    public void SerializeAsV1_WithReference_WritesReference()
    {
        var failureAction = new ArazzoFailureActionReference("shared");

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        failureAction.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.failureActions.shared", json?["reference"]?.GetValue<string>());
    }

    [Fact]
    public void Deserialize_WithReference_ReturnsFailureActionReference()
    {
        var json = """
        {
            "reference": "$components.failureActions.shared"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureActionReference>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal("$components.failureActions.shared", failureAction.Reference.ReferenceV1);
        Assert.Null(failureAction.Criteria);
    }

    [Fact]
    public void Deserialize_WithDollarRef_ReturnsFailureActionObject()
    {
        var json = """
        {
            "$ref": "$components.failureActions.shared"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureAction>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Null(failureAction.Name);
        Assert.Null(failureAction.Type);
    }

    [Theory]
    [InlineData("$components.parameters.shared")]
    [InlineData("$components.failureActions")]
    public void Deserialize_WithInvalidReusableReference_AddsDiagnosticError(string reference)
    {
        var json = $$"""
        {
            "reference": "{{reference}}"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = Assert.IsType<ArazzoFailureActionReference>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("$components.failureActions.<name>", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithExternalReference_ThrowsOpenApiException()
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "reference": "external.json#$components.failureActions.shared"
            }
            """)!;

        var exception = Assert.Throws<OpenApiException>(() => ArazzoV1Deserializer.LoadFailureAction(jsonNode, new ParsingContext(new())));

        Assert.Contains("do not support external resources", exception.Message, StringComparison.Ordinal);
    }
}