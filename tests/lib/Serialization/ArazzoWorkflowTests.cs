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
            Inputs = new ArazzoInput { Type = JsonSchemaType.Object },
            DependsOn = new HashSet<string> { "authWorkflow", "setupWorkflow" },
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep
                {
                    StepId = "step1",
                    OperationPath = "/users/{id}"
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
            Parameters = new Dictionary<string, IArazzoParameter>
            {
                ["userId"] = new ArazzoParameter
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
                    "operationPath": "/users/{id}"
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
            "parameters": {
                "userId": {
                    "name": "userId",
                    "in": "path",
                    "value": "123"
                }
            },
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
            WorkflowId = "minimalWorkflow"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "workflowId": "minimalWorkflow"
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
        Assert.NotNull(workflow.Inputs);
        Assert.Equal(JsonSchemaType.Object, workflow.Inputs!.Type);

        Assert.NotNull(workflow.DependsOn);
        Assert.Contains("workflow1", workflow.DependsOn);
        Assert.Contains("workflow2", workflow.DependsOn);
    }

    [Fact]
    public void SerializeAsV1_ShouldHandleNullCollections()
    {
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "emptyWorkflow"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "workflowId": "emptyWorkflow"
        }
        """;

        workflow.SerializeAsV1(writer);
        var result = textWriter.ToString();
        var jsonResultObject = JsonNode.Parse(result);
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
    }

    [Fact]
    public void Deserialize_WithReferences_LoadsReferenceTypes()
    {
        var json = """
        {
            "workflowId": "referenceWorkflow",
            "successActions": [
                {
                    "$ref": "$components.successActions.successAction"
                }
            ],
            "failureActions": [
                {
                    "$ref": "$components.failureActions.failureAction"
                }
            ],
            "parameters": {
                "userId": {
                    "$ref": "$components.parameters.userId",
                    "value": "7"
                }
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());

        var workflow = ArazzoV1Deserializer.LoadWorkflow(jsonNode, parsingContext);

        Assert.IsType<ArazzoSuccessActionReference>(Assert.Single(workflow.SuccessActions!));
        Assert.IsType<ArazzoFailureActionReference>(Assert.Single(workflow.FailureActions!));
        var parameter = Assert.IsType<ArazzoParameterReference>(workflow.Parameters!["userId"]);
        Assert.Equal("7", parameter.Value?.GetValue<string>());
    }
}