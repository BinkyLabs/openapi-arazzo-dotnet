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
            StepId = "step456",
            RetryAfter = 5.5m,
            RetryLimit = 3ul,
            Criteria = new List<ArazzoCriterion>
            {
                new ArazzoCriterion
                {
                    Context = "$response.statusCode",
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
            "stepId": "step456",
            "retryAfter": 5.5,
            "retryLimit": 3,
            "criteria": [
                {
                    "context": "$response.statusCode",
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
            WorkflowId = "workflow456",
            StepId = "step789"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "name": "gotoAction",
            "type": "goto",
            "workflowId": "workflow456",
            "stepId": "step789"
        }
        """;

        failureAction.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
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
            "stepId": "retryStep",
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
        Assert.Equal("retryStep", failureAction.StepId);
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
        Assert.Null(failureAction.RetryLimit);
        Assert.Null(failureAction.Criteria);
        Assert.Null(failureAction.Extensions);
    }

    [Fact]
    public void Deserialize_ShouldHandleGotoType()
    {
        var json = """
        {
            "name": "gotoFailure",
            "type": "goto",
            "workflowId": "errorWorkflow",
            "stepId": "errorHandler"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureAction>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal("gotoFailure", failureAction.Name);
        Assert.Equal(ArazzoFailureType.Goto, failureAction.Type);
        Assert.Equal("errorWorkflow", failureAction.WorkflowId);
        Assert.Equal("errorHandler", failureAction.StepId);
        Assert.Null(failureAction.RetryAfter);
        Assert.Null(failureAction.RetryLimit);
        Assert.Null(failureAction.Criteria);
        Assert.Null(failureAction.Extensions);
    }

    [Fact]
    public void SerializeAsV1_WithReference_WritesDollarRef()
    {
        var failureAction = new ArazzoFailureActionReference("shared");

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        failureAction.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.failureActions.shared", json?["$ref"]?.GetValue<string>());
    }

    [Fact]
    public void Deserialize_WithReference_ReturnsFailureActionReference()
    {
        var json = """
        {
            "$ref": "$components.failureActions.shared"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var failureAction = Assert.IsType<ArazzoFailureActionReference>(ArazzoV1Deserializer.LoadFailureAction(jsonNode, parsingContext));

        Assert.Equal("$components.failureActions.shared", failureAction.Reference.ReferenceV1);
        Assert.Null(failureAction.Criteria);
    }

    [Fact]
    public void Deserialize_WithExternalReference_ThrowsOpenApiException()
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "$ref": "external.json#$components.failureActions.shared"
            }
            """)!;

        var exception = Assert.Throws<OpenApiException>(() => ArazzoV1Deserializer.LoadFailureAction(jsonNode, new ParsingContext(new())));

        Assert.Contains("do not support external resources", exception.Message, StringComparison.Ordinal);
    }
}