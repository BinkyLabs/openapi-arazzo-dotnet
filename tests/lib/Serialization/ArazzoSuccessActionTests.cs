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
            WorkflowId = "workflow123",
            StepId = "step456",
            Criteria = new List<ArazzoCriterion>
            {
                new ArazzoCriterion
                {
                    Context = "$response.statusCode",
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
            "workflowId": "workflow123",
            "stepId": "step456",
            "criteria": [
                {
                    "context": "$response.statusCode",
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
            "stepId": "nextStep",
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

        var successAction = ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext);

        Assert.Equal("gotoAction", successAction.Name);
        Assert.Equal(ArazzoSuccessType.Goto, successAction.Type);
        Assert.Equal("mainWorkflow", successAction.WorkflowId);
        Assert.Equal("nextStep", successAction.StepId);
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

        var successAction = ArazzoV1Deserializer.LoadSuccessAction(jsonNode, parsingContext);

        Assert.Equal("simpleEnd", successAction.Name);
        Assert.Equal(ArazzoSuccessType.End, successAction.Type);
        Assert.Null(successAction.WorkflowId);
        Assert.Null(successAction.StepId);
        Assert.Null(successAction.Criteria);
        Assert.Null(successAction.Extensions);
    }
}