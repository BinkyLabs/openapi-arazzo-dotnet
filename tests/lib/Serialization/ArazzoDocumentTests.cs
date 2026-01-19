using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoDocumentTests
{
    [Fact]
    public void SerializeAsV1_ShouldWriteCorrectJson()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Arazzo = "1.0.1",
            Info = new ArazzoInfo
            {
                Title = "Test Overlay",
                Version = "1.0.0"
            },
            SourceDescriptions = new List<ArazzoSourceDescription>
            {
                new ArazzoSourceDescription
                {
                    Name = "source1",
                    Url = new Uri("https://example.com/api"),
                    Type = ArazzoDescriptionType.OpenAPI
                }
            },
            Workflows = new List<ArazzoWorkflow>
            {
                new ArazzoWorkflow
                {
                    WorkflowId = "testWorkflow",
                    Summary = "Test workflow"
                }
            },
            Components = new ArazzoComponent
            {
                Parameters = new Dictionary<string, ArazzoParameter>
                {
                    ["testParam"] = new ArazzoParameter
                    {
                        Name = "testParam",
                        In = ParameterLocation.Header,
                        Value = "test-value"
                    }
                }
            },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-custom"] = new JsonNodeExtension(JsonNode.Parse("\"document-extension\"")!)
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test Overlay",
                "version": "1.0.0"
            },
            "sourceDescriptions": [
                {
                    "name": "source1",
                    "url": "https://example.com/api",
                    "type": "openapi"
                }
            ],
            "workflows": [
                {
                    "workflowId": "testWorkflow",
                    "summary": "Test workflow"
                }
            ],
            "components": {
                "parameters": {
                    "testParam": {
                        "name": "testParam",
                        "in": "header",
                        "value": "test-value"
                    }
                }
            },
            "x-custom": "document-extension"
        }
        """;

        // Act
        document.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Fact]
    public void SerializeAsV1_MinimalDocument_ShouldWriteCorrectJson()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo
            {
                Title = "Minimal Overlay",
                Version = "1.0.0"
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Minimal Overlay",
                "version": "1.0.0"
            }
        }
        """;

        // Act
        document.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var json = """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test Overlay",
                "version": "1.0.0"
            },
            "sourceDescriptions": [
                {
                    "name": "source1",
                    "url": "https://example.com/api",
                    "type": "openapi"
                }
            ],
            "workflows": [
                {
                    "workflowId": "testWorkflow",
                    "summary": "Test workflow"
                }
            ],
            "components": {
                "parameters": {
                    "testParam": {
                        "name": "testParam",
                        "in": "header",
                        "value": "test-value"
                    }
                }
            }
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        // Act
        var document = ArazzoV1Deserializer.LoadDocument(parseNode);

        // Assert
        Assert.Equal("1.0.1", document.Arazzo);
        Assert.NotNull(document.Info);
        Assert.Equal("Test Overlay", document.Info.Title);
        Assert.Equal("1.0.0", document.Info.Version);

        Assert.NotNull(document.SourceDescriptions);
        Assert.Single(document.SourceDescriptions);
        Assert.Equal("source1", document.SourceDescriptions[0].Name);
        Assert.Equal("https://example.com/api", document.SourceDescriptions[0].Url?.ToString());

        Assert.NotNull(document.Workflows);
        Assert.Single(document.Workflows);
        Assert.Equal("testWorkflow", document.Workflows[0].WorkflowId);
        Assert.Equal("Test workflow", document.Workflows[0].Summary);

        Assert.NotNull(document.Components);
        Assert.NotNull(document.Components.Parameters);
        Assert.True(document.Components.Parameters.ContainsKey("testParam"));
    }

    [Fact]
    public void SerializeAsV1_ShouldHandleNullCollections()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo
            {
                Title = "Test",
                Version = "1.0.0"
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test",
                "version": "1.0.0"
            }
        }
        """;

        // Act
        document.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Fact]
    public void SerializeAsV1_ShouldHandleEmptyCollections()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo
            {
                Title = "Test",
                Version = "1.0.0"
            },
            SourceDescriptions = new List<ArazzoSourceDescription>(),
            Workflows = new List<ArazzoWorkflow>()
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test",
                "version": "1.0.0"
            }
        }
        """;

        // Act
        document.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Fact]
    public void Deserialize_WithExtensions_ShouldLoadExtensions()
    {
        // Arrange
        var json = """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test",
                "version": "1.0.0"
            },
            "x-custom": "value"
        }
        """;
        var jsonNode = JsonNode.Parse(json)!;
        var parsingContext = new ParsingContext(new());
        var parseNode = new MapNode(parsingContext, jsonNode);

        // Act
        var document = ArazzoV1Deserializer.LoadDocument(parseNode);

        // Assert
        Assert.NotNull(document.Extensions);
        Assert.True(document.Extensions.ContainsKey("x-custom"));
    }
}
