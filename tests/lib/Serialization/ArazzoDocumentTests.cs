using System.Text;
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
                Title = "Test Arazzo",
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
                    Summary = "Test workflow",
                    Steps = new List<ArazzoStep>
                    {
                        new ArazzoStep { StepId = "step1", OperationId = "getUser" }
                    }
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
                "title": "Test Arazzo",
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
                    "summary": "Test workflow",
                    "steps": [
                        {
                            "stepId": "step1",
                            "operationId": "getUser"
                        }
                    ]
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
                Title = "Minimal Arazzo",
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
                    WorkflowId = "workflow1",
                    Steps = new List<ArazzoStep>
                    {
                        new ArazzoStep { StepId = "step1", OperationId = "getUser" }
                    }
                }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var expectedJson =
        """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Minimal Arazzo",
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
                    "workflowId": "workflow1",
                    "steps": [
                        {
                            "stepId": "step1",
                            "operationId": "getUser"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        document.SerializeAsV1(writer);
        var jsonResultObject = JsonNode.Parse(textWriter.ToString());
        var expectedJsonObject = JsonNode.Parse(expectedJson);

        // Assert
        Assert.True(JsonNode.DeepEquals(jsonResultObject, expectedJsonObject), "The serialized JSON does not match the expected JSON.");
    }

    [Theory]
    [MemberData(nameof(UnresolvedSemanticReferenceDocuments))]
    public void SerializeAsV1_WithUnresolvedSemanticReference_ShouldThrowArazzoSerializationException(ArazzoDocument document, string expectedMessage)
    {
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> UnresolvedSemanticReferenceDocuments()
    {
        yield return
        [
            CreateDocument(new ArazzoStep { StepId = "step1", WorkflowId = "missingWorkflow" }),
            "references unknown workflowId 'missingWorkflow'"
        ];
        yield return
        [
            CreateDocument(
                new ArazzoStep { StepId = "step1", OperationId = "getUser" },
                successActions: [new ArazzoSuccessAction { Name = "goto", Type = ArazzoSuccessType.Goto, StepId = "missingStep" }]),
            "references unknown stepId 'missingStep'"
        ];
        yield return
        [
            CreateDocument(
                new ArazzoStep
                {
                    StepId = "step1",
                    Parameters = [new ArazzoParameterReference("missing")]
                }),
            "reference '$components.parameters.missing' does not resolve"
        ];
        yield return
        [
            CreateDocument(new ArazzoStep { StepId = "step1", OperationPath = "{$sourceDescriptions.missing.url}#/paths/~1users/get" }),
            "references unknown sourceDescription 'missing'"
        ];
        yield return
        [
            CreateDocument(new ArazzoStep { StepId = "step1" }, dependsOn: new HashSet<string> { "missingWorkflow" }),
            "dependsOn references unknown workflowId 'missingWorkflow'"
        ];
        yield return
        [
            CreateDocument(new ArazzoStep { StepId = "step1" }, dependsOn: new HashSet<string> { "$sourceDescriptions.missing.externalWorkflow" }),
            "dependsOn value '$sourceDescriptions.missing.externalWorkflow' references unknown sourceDescription 'missing'"
        ];
        yield return
        [
            CreateDocument(new ArazzoStep { StepId = "step1" }, dependsOn: new HashSet<string> { "$steps.step1" }),
            "dependsOn value '$steps.step1' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'"
        ];
    }

    [Fact]
    public void SerializeAsV1_WithResolvedSemanticReferences_ShouldSerialize()
    {
        var document = CreateDocument(
            new ArazzoStep
            {
                StepId = "step1",
                OperationPath = "{$sourceDescriptions.source1.url}#/paths/~1users/get",
                Parameters = [new ArazzoParameterReference("shared")]
            },
            successActions: [new ArazzoSuccessAction { Name = "goto", Type = ArazzoSuccessType.Goto, StepId = "step1" }],
            parameters: new Dictionary<string, ArazzoParameter>
            {
                ["shared"] = new ArazzoParameter { Name = "id", In = ParameterLocation.Query, Value = "1" }
            },
            dependsOn: new HashSet<string> { "child", "$sourceDescriptions.source1.externalWorkflow" });
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        document.SerializeAsV1(writer);
        var json = JsonNode.Parse(textWriter.ToString())!;

        Assert.NotNull(json["workflows"]);
    }

    private static ArazzoDocument CreateDocument(
        ArazzoStep step,
        IList<IArazzoSuccessAction>? successActions = null,
        IDictionary<string, ArazzoParameter>? parameters = null,
        ISet<string>? dependsOn = null)
    {
        return new ArazzoDocument
        {
            Info = new ArazzoInfo
            {
                Title = "Test Arazzo",
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
                    WorkflowId = "workflow1",
                    DependsOn = dependsOn,
                    Steps = new List<ArazzoStep> { step },
                    SuccessActions = successActions
                },
                new ArazzoWorkflow
                {
                    WorkflowId = "child",
                    Steps = new List<ArazzoStep>
                    {
                        new ArazzoStep { StepId = "childStep", OperationId = "getUser" }
                    }
                }
            },
            Components = parameters is null
                ? null
                : new ArazzoComponent
                {
                    Parameters = parameters
                }
        };
    }

    [Fact]
    public void Deserialize_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var json = """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test Arazzo",
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

        // Act
        var document = ArazzoV1Deserializer.LoadDocument(jsonNode, parsingContext);

        // Assert
        Assert.Equal("1.0.1", document.Arazzo);
        Assert.NotNull(document.Info);
        Assert.Equal("Test Arazzo", document.Info.Title);
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

        // Act
        var document = ArazzoV1Deserializer.LoadDocument(jsonNode, parsingContext);

        // Assert
        Assert.NotNull(document.Extensions);
        Assert.True(document.Extensions.ContainsKey("x-custom"));
    }

    [Fact]
    public void SerializeAsV1_WithNullInfo_ShouldThrowArazzoSerializationException()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = null,
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
                new ArazzoWorkflow { WorkflowId = "workflow1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        // Act & Assert
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));
        Assert.Equal("Info is required for ArazzoDocument serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithNullSourceDescriptions_ShouldThrowArazzoSerializationException()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo { Title = "Test", Version = "1.0.0" },
            SourceDescriptions = null,
            Workflows = new List<ArazzoWorkflow>
            {
                new ArazzoWorkflow { WorkflowId = "workflow1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        // Act & Assert
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));
        Assert.Equal("SourceDescriptions is required and must contain at least one element for ArazzoDocument serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithEmptySourceDescriptions_ShouldThrowArazzoSerializationException()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo { Title = "Test", Version = "1.0.0" },
            SourceDescriptions = new List<ArazzoSourceDescription>(),
            Workflows = new List<ArazzoWorkflow>
            {
                new ArazzoWorkflow { WorkflowId = "workflow1" }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        // Act & Assert
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));
        Assert.Equal("SourceDescriptions is required and must contain at least one element for ArazzoDocument serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithNullWorkflows_ShouldThrowArazzoSerializationException()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo { Title = "Test", Version = "1.0.0" },
            SourceDescriptions = new List<ArazzoSourceDescription>
            {
                new ArazzoSourceDescription
                {
                    Name = "source1",
                    Url = new Uri("https://example.com/api"),
                    Type = ArazzoDescriptionType.OpenAPI
                }
            },
            Workflows = null
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        // Act & Assert
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));
        Assert.Equal("Workflows is required and must contain at least one element for ArazzoDocument serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithEmptyWorkflows_ShouldThrowArazzoSerializationException()
    {
        // Arrange
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo { Title = "Test", Version = "1.0.0" },
            SourceDescriptions = new List<ArazzoSourceDescription>
            {
                new ArazzoSourceDescription
                {
                    Name = "source1",
                    Url = new Uri("https://example.com/api"),
                    Type = ArazzoDescriptionType.OpenAPI
                }
            },
            Workflows = new List<ArazzoWorkflow>()
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        // Act & Assert
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));
        Assert.Equal("Workflows is required and must contain at least one element for ArazzoDocument serialization.", exception.Message);
    }

    [Fact]
    public void SerializeAsV1_WithDuplicateSourceDescriptionNames_ShouldThrowArazzoSerializationException()
    {
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo { Title = "Test", Version = "1.0.0" },
            SourceDescriptions = new List<ArazzoSourceDescription>
            {
                new ArazzoSourceDescription { Name = "source1", Url = new Uri("https://example.com/api1") },
                new ArazzoSourceDescription { Name = "source1", Url = new Uri("https://example.com/api2") }
            },
            Workflows = new List<ArazzoWorkflow>
            {
                new ArazzoWorkflow
                {
                    WorkflowId = "workflow1",
                    Steps = new List<ArazzoStep> { new ArazzoStep { StepId = "step1", OperationId = "getUser" } }
                }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));

        Assert.Contains("duplicate name 'source1'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeAsV1_WithDuplicateWorkflowIds_ShouldThrowArazzoSerializationException()
    {
        var document = new ArazzoDocument
        {
            Info = new ArazzoInfo { Title = "Test", Version = "1.0.0" },
            SourceDescriptions = new List<ArazzoSourceDescription>
            {
                new ArazzoSourceDescription { Name = "source1", Url = new Uri("https://example.com/api") }
            },
            Workflows = new List<ArazzoWorkflow>
            {
                new ArazzoWorkflow
                {
                    WorkflowId = "workflow1",
                    Steps = new List<ArazzoStep> { new ArazzoStep { StepId = "step1", OperationId = "getUser" } }
                },
                new ArazzoWorkflow
                {
                    WorkflowId = "workflow1",
                    Steps = new List<ArazzoStep> { new ArazzoStep { StepId = "step2", OperationId = "getUser" } }
                }
            }
        };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1(writer));

        Assert.Contains("duplicate workflowId 'workflow1'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadFromStreamAsync_ShouldParseDocument()
    {
        const string json =
            """
            {
              "arazzo": "1.0.0",
              "info": {
                "title": "Loaded from stream",
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
                  "workflowId": "workflow1",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ]
                }
              ]
            }
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await ArazzoDocument.LoadFromStreamAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result.Document);
        Assert.Equal("Loaded from stream", result.Document!.Info!.Title);
    }

    [Fact]
    public async Task ParseAsync_ShouldParseDocument()
    {
        const string json =
            """
            {
              "arazzo": "1.0.0",
              "info": {
                "title": "Parsed document",
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
                  "workflowId": "workflow1",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ]
                }
              ]
            }
            """;

        var result = await ArazzoDocument.ParseAsync(json, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result.Document);
        Assert.Equal("Parsed document", result.Document!.Info!.Title);
    }

    [Fact]
    public async Task LoadFromUrlAsync_WithLocalFile_ShouldParseDocument()
    {
        const string json =
            """
            {
              "arazzo": "1.0.0",
              "info": {
                "title": "Loaded from file",
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
                  "workflowId": "workflow1",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ]
                }
              ]
            }
            """;

        var filePath = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, json, TestContext.Current.CancellationToken);

        try
        {
            var result = await ArazzoDocument.LoadFromUrlAsync(filePath, token: TestContext.Current.CancellationToken);

            Assert.NotNull(result.Document);
            Assert.Equal("Loaded from file", result.Document!.Info!.Title);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}