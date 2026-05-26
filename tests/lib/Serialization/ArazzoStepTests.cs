using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoStepTests
{
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
            OperationPath = "/users/{id}"
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "stepId": "step1",
            "operationPath": "/users/{id}"
        }
        """;

        step.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "Serialized JSON does not match expected output.");
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
}