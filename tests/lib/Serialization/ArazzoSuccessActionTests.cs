using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoSuccessActionTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson_WithAllProperties()
    {
        var successAction = new ArazzoSuccessAction
        {
            Name = "successAction1",
            Type = ArazzoSuccessType.Goto,
            StepId = "step456",
            Criteria = new List<ArazzoCriterion>
            {
                new ArazzoCriterion
                {
                    Context = "$statusCode",
                    Condition = "200"
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
            "name": "successAction1",
            "type": "goto",
            "stepId": "step456",
            "criteria": [
                {
                    "context": "$statusCode",
                    "condition": "200"
                }
            ],
            "x-extra": {
                "note": "test"
            }
        }
        """;

        successAction.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson_WithRequiredPropertiesOnly()
    {
        var successAction = new ArazzoSuccessAction
        {
            Name = "endAction",
            Type = ArazzoSuccessType.End
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

        successAction.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void SerializeAsV1_ShouldThrowException_WhenNameIsNull()
    {
        var successAction = new ArazzoSuccessAction
        {
            Type = ArazzoSuccessType.End
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => successAction.SerializeAsV1(writer));
    }

    [Fact]
    public void SerializeAsV1_WithWorkflowIdAndStepId_ShouldThrowArazzoSerializationException()
    {
        var successAction = new ArazzoSuccessAction
        {
            Name = "gotoAction",
            Type = ArazzoSuccessType.Goto,
            WorkflowId = "workflow1",
            StepId = "step1"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => successAction.SerializeAsV1(writer));

        Assert.Contains("can define only one of workflowId or stepId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithEndAndTargetField_ShouldThrowArazzoSerializationException()
    {
        var successAction = new ArazzoSuccessAction
        {
            Name = "endAction",
            Type = ArazzoSuccessType.End,
            WorkflowId = "workflow1"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => successAction.SerializeAsV1(writer));

        Assert.Contains("type=end must not define workflowId or stepId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithGotoAndNoTargetField_ShouldThrowArazzoSerializationException()
    {
        var successAction = new ArazzoSuccessAction
        {
            Name = "gotoAction",
            Type = ArazzoSuccessType.Goto
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => successAction.SerializeAsV1(writer));

        Assert.Contains("type=goto must define exactly one of workflowId or stepId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_ShouldThrowException_WhenTypeIsNull()
    {
        var successAction = new ArazzoSuccessAction
        {
            Name = "testAction"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        Assert.Throws<ArgumentNullException>(() => successAction.SerializeAsV1(writer));
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesAndExtensions()
    {
        var json = """
        {
            "name": "gotoAction",
            "type": "goto",
            "workflowId": "mainWorkflow",
            "criteria": [
                {
                    "context": "$response.body#/success",
                    "condition": "true"
                }
            ],
            "x-flag": true
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var successAction = Assert.IsType<ArazzoSuccessAction>(ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext));

        Assert.Equal("gotoAction", successAction.Name);
        Assert.Equal(ArazzoSuccessType.Goto, successAction.Type);
        Assert.Equal("mainWorkflow", successAction.WorkflowId);
        Assert.Null(successAction.StepId);
        Assert.NotNull(successAction.Criteria);
        Assert.Single(successAction.Criteria);
        Assert.Equal("$response.body#/success", successAction.Criteria[0].Context);
        Assert.Equal("true", successAction.Criteria[0].Condition);
        Assert.NotNull(successAction.Extensions);
        var extension = Assert.IsType<JsonNodeExtension>(successAction.Extensions!["x-flag"]);
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

        var successAction = Assert.IsType<ArazzoSuccessAction>(ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext));

        Assert.Equal("simpleEnd", successAction.Name);
        Assert.Equal(ArazzoSuccessType.End, successAction.Type);
        Assert.Null(successAction.WorkflowId);
        Assert.Null(successAction.StepId);
        Assert.Null(successAction.Criteria);
        Assert.Null(successAction.Extensions);
    }

    [Theory]
    [InlineData("""{ "name": "endAction", "type": "end", "workflowId": "workflow1" }""", "type=end must not define workflowId or stepId")]
    [InlineData("""{ "name": "gotoAction", "type": "goto" }""", "type=goto must define exactly one of workflowId or stepId")]
    [InlineData("""{ "name": "gotoAction", "type": "goto", "workflowId": "workflow1", "stepId": "step1" }""", "can define only one of workflowId or stepId")]
    public void Deserialize_WithInvalidTypeDependentTargetFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext);

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void SerializeAsV1_WithReference_WritesReference()
    {
        var successAction = new ArazzoSuccessActionReference("shared");

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        successAction.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.successActions.shared", json?["reference"]?.GetValue<string>());
    }

    [Fact]
    public void Deserialize_WithReference_ReturnsSuccessActionReference()
    {
        var json = """
        {
            "reference": "$components.successActions.shared"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var successAction = Assert.IsType<ArazzoSuccessActionReference>(ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext));

        Assert.Equal("$components.successActions.shared", successAction.Reference.ReferenceV1);
        Assert.Null(successAction.Criteria);
    }

    [Fact]
    public void Deserialize_WithDollarRef_ReturnsSuccessActionObject()
    {
        var json = """
        {
            "$ref": "$components.successActions.shared"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var successAction = Assert.IsType<ArazzoSuccessAction>(ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext));

        Assert.Null(successAction.Name);
        Assert.Null(successAction.Type);
    }

    [Theory]
    [InlineData("$components.parameters.shared")]
    [InlineData("$components.successActions")]
    public void Deserialize_WithInvalidReusableReference_AddsDiagnosticError(string reference)
    {
        var json = $$"""
        {
            "reference": "{{reference}}"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        _ = Assert.IsType<ArazzoSuccessActionReference>(ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext));

        Assert.Contains(parsingContext.Diagnostic.Errors, error => error.Message.Contains("$components.successActions.<name>", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_WithExternalReference_ThrowsOpenApiException()
    {
        var jsonNode = JsonNode.Parse(
            """
            {
                "reference": "external.json#$components.successActions.shared"
            }
            """)!;

        var exception = Assert.Throws<OpenApiException>(() => ArazzoV1Deserializer.LoadSuccessAction(jsonNode, new ParsingContext(new())));

        Assert.Contains("do not support external resources", exception.Message, StringComparison.Ordinal);
    }
}