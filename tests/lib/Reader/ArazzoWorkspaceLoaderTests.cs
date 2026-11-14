using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoWorkspaceLoaderTests
{
    [Theory]
    [InlineData("https://example.com/canonical/arazzo.json", true)]
    [InlineData("https://example.com/canonical/arazzo.json", false)]
    [InlineData("../canonical/arazzo.json", true)]
    [InlineData("../canonical/arazzo.json", false)]
    public async Task LoadAsync_SelfResolvesSourcesAndExternalSchemasBeforeRegistration(string self, bool validate)
    {
        var json = JsonNode.Parse("""
            {
              "arazzo": "1.1.0",
              "info": { "title": "Test", "version": "1" },
              "sourceDescriptions": [{ "name": "api", "url": "api.json", "type": "openapi" }],
              "workflows": [{
                "workflowId": "wf",
                "inputs": {
                  "type": "object",
                  "properties": {
                    "local": { "$ref": "#/components/inputs/count" },
                    "external": { "$ref": "external-schema.json#/$defs/shared" }
                  }
                },
                "steps": [{ "stepId": "step", "operationId": "op" }]
              }],
              "components": { "inputs": { "count": { "type": "integer" } } }
            }
            """);
        Assert.NotNull(json);
        json["$self"] = self;
        var settings = new ArazzoReaderSettings();
        settings.OpenApiSettings.LoadExternalRefs = true;
        settings.OpenApiSettings.RuleSet = validate ? ValidationRuleSet.GetDefaultRuleSet() : ValidationRuleSet.GetEmptyRuleSet();
        settings.OpenApiSettings.CustomExternalLoader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/canonical/api.json"] = """
                {
                  "openapi": "3.1.0",
                  "info": { "title": "API", "version": "1" },
                  "paths": { "/items": { "get": { "operationId": "op", "responses": { "200": { "description": "OK" } } } } }
                }
                """,
            ["https://example.com/canonical/external-schema.json"] = """
                { "$schema": "https://json-schema.org/draft/2020-12/schema", "$defs": { "shared": { "type": "string" } } }
                """
        });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.ToJsonString()));

        var result = await ArazzoModelFactory.LoadFromStreamAsync(stream, OpenApiConstants.Json, settings,
            TestContext.Current.CancellationToken, new Uri("https://example.com/cache/arazzo.json"));

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(self, result.Document.Self);
        Assert.Equal(new Uri("https://example.com/canonical/arazzo.json"), result.Document.BaseUri);
        var workspace = result.Document.Workspace;
        Assert.NotNull(workspace);
        Assert.True(workspace.TryGetSourceDescription("api", out var source));
        Assert.Equal(new Uri("https://example.com/canonical/api.json"), source.Uri);
        Assert.True(workspace.IsOpenApiDocumentLoaded(source.Uri));
        Assert.NotNull(result.Document.Workflows);
        var inputs = Assert.IsType<ArazzoInput>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(inputs.Properties);
        var local = Assert.IsType<ArazzoInputReference>(inputs.Properties["local"]);
        Assert.NotNull(local.Target);
        Assert.Equal(JsonSchemaType.Integer, local.Target.Type);
        var external = Assert.IsType<ArazzoInputReference>(inputs.Properties["external"]);
        Assert.NotNull(external.Target);
        Assert.Equal(JsonSchemaType.String, external.Target.Type);
    }

    [Fact]
    public async Task LoadAsync_ExternalArazzoDocumentUsesItsOwnSelfForNestedReferences()
    {
        var document = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Workflows =
            [
                new ArazzoWorkflow
                {
                    WorkflowId = "wf",
                    Inputs = CreateExternalSchemaReference("shared", "external.arazzo.json", "#/components/inputs/shared")
                }
            ]
        };
        var reference = Assert.IsType<ArazzoInputReference>(document.Workflows[0].Inputs);
        reference.Reference.EnsureHostDocumentIsSet(document);
        var workspace = document.Workspace;
        Assert.NotNull(workspace);
        var loader = new ArazzoWorkspaceLoader(workspace, new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/external.arazzo.json"] = """
                {
                  "arazzo": "1.1.0",
                  "info": { "title": "External", "version": "1" },
                  "sourceDescriptions": [{ "name": "api", "url": "https://example.com/api.json", "type": "openapi" }],
                  "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step", "operationId": "op" }] }],
                  "components": { "inputs": { "shared": { "$ref": "leaf.json#/$defs/leaf" } } },
                  "$self": "https://other.example.com/canonical/arazzo.json"
                }
                """,
            ["https://other.example.com/canonical/leaf.json"] = """
                { "$schema": "https://json-schema.org/draft/2020-12/schema", "$defs": { "leaf": { "type": "string" } } }
                """
        }), new());

        await loader.LoadAsync(new BaseArazzoReference { ExternalResource = "/" }, document, TestContext.Current.CancellationToken);

        Assert.Equal(new Uri("https://other.example.com/canonical/arazzo.json"), workspace.GetDocumentId("external.arazzo.json"));
        Assert.NotNull(reference.Target);
        Assert.Equal(JsonSchemaType.String, reference.Target.Type);
    }

    [Fact]
    public async Task LoadAsync_LoadsExternalDocumentsAndRegistersTheirComponents()
    {
        const string externalDocument = """
            {
              "arazzo": "1.0.1",
              "info": {
                "title": "External",
                "version": "1.0.0"
              },
              "sourceDescriptions": [
                {
                  "name": "api",
                  "type": "openapi",
                  "url": "https://example.com/openapi.yaml"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step",
                      "operationId": "op"
                    }
                  ]
                }
              ],
              "components": {
                "inputs": {
                  "shared": {
                    "type": "string"
                  }
                }
              }
            }
            """;

        var mainDocument = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Components = new ArazzoComponent
            {
                Inputs = new Dictionary<string, IArazzoInput>
                {
                    ["local"] = new ArazzoInput { Type = JsonSchemaType.String }
                }
            },
            Workflows =
            [
                new ArazzoWorkflow
                {
                    Inputs = new ArazzoInputReference("shared", null, "external.json")
                }
            ]
        };
        mainDocument.Workflows[0].Inputs = CreateNestedExternalReferenceGraph(mainDocument);

        var workspace = new ArazzoWorkspace(new Uri("https://example.com/root/arazzo.json"));
        mainDocument.Workspace = workspace;
        var loader = new ArazzoWorkspaceLoader(
            workspace,
            new TestStreamLoader(new Dictionary<string, string>
            {
                ["https://example.com/root/external.json"] = externalDocument
            }),
            new ArazzoReaderSettings());

        await loader.LoadAsync(new BaseArazzoReference { ExternalResource = "/" }, mainDocument, TestContext.Current.CancellationToken);

        Assert.True(workspace.ComponentsCount() >= 1);
        Assert.Same(workspace, mainDocument.Workspace);
    }

    [Fact]
    public async Task LoadAsync_IgnoresNullDocument()
    {
        var workspace = new ArazzoWorkspace(new Uri("https://example.com/root/arazzo.json"));
        var loader = new ArazzoWorkspaceLoader(workspace, new TestStreamLoader(new Dictionary<string, string>()), new ArazzoReaderSettings());

        await loader.LoadAsync(new BaseArazzoReference { ExternalResource = "/" }, null, TestContext.Current.CancellationToken);

        Assert.Equal(0, workspace.ComponentsCount());
    }

    [Fact]
    public async Task LoadAsync_ExternalInputReferencedAndReExported_DoesNotProduceCircularReferenceError()
    {
        var tempDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDirectory);

            var rootPath = Path.Join(tempDirectory, "root.arazzo.yaml");
            var sharedPath = Path.Join(tempDirectory, "shared.arazzo.yaml");

            await File.WriteAllTextAsync(rootPath,
            @"arazzo: 1.0.1
info:
  title: T
  version: 1.0.0
sourceDescriptions:
  - name: api
    type: openapi
    url: https://example.com/api.yaml
workflows:
  - workflowId: wf
    inputs:
      $ref: './shared.arazzo.yaml#/components/inputs/Leaf'
    steps:
      - stepId: step
        operationId: op
components:
  inputs:
    Leaf:
      $ref: './shared.arazzo.yaml#/components/inputs/Leaf'
", TestContext.Current.CancellationToken);

            await File.WriteAllTextAsync(sharedPath,
            @"arazzo: 1.0.1
info:
  title: Shared
  version: 1.0.0
sourceDescriptions:
  - name: api
    type: openapi
    url: https://example.com/api.yaml
workflows:
  - workflowId: wf
    steps:
      - stepId: step
        operationId: op
components:
  inputs:
    Leaf:
      type: object
      properties:
        x:
          type: string
        y:
          type: integer
", TestContext.Current.CancellationToken);

            var settings = new ArazzoReaderSettings();
            settings.OpenApiSettings.LoadExternalRefs = true;
            settings.OpenApiSettings.RuleSet = ValidationRuleSet.GetEmptyRuleSet();

            var result = await ArazzoModelFactory.LoadFormUrlAsync(rootPath, settings, TestContext.Current.CancellationToken);

            Assert.NotNull(result.Document);
            var errors = result.Diagnostic?.Errors ?? [];
            Assert.DoesNotContain(errors, error => error.Message.Contains("Circular reference detected while resolving schema", StringComparison.Ordinal));
            Assert.IsType<ArazzoInputReference>(result.Document.Workflows![0].Inputs);
            Assert.IsType<ArazzoInputReference>(result.Document.Components!.Inputs!["Leaf"]);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_ExternalJsonSchemaDocument_RegistersRootDefsAndResolvedSubSchemas()
    {
        const string externalSchemaDocument = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "$defs": {
                "shared": {
                  "type": "string"
                }
              },
              "properties": {
                "value": {
                  "$ref": "#/$defs/shared"
                },
                "count": {
                  "type": "integer"
                }
              }
            }
            """;

        var mainDocument = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Workflows =
            [
                new ArazzoWorkflow
                {
                    WorkflowId = "wf",
                    Inputs = CreateExternalSchemaReference("shared", "external-schema.json", "#/$defs/shared")
                }
            ]
        };
        ((ArazzoInputReference)mainDocument.Workflows[0].Inputs!).Reference.EnsureHostDocumentIsSet(mainDocument);

        var workspace = new ArazzoWorkspace(mainDocument.BaseUri);
        mainDocument.Workspace = workspace;
        var loader = new ArazzoWorkspaceLoader(
            workspace,
            new TestStreamLoader(new Dictionary<string, string>
            {
                ["https://example.com/root/external-schema.json"] = externalSchemaDocument
            }),
            new ArazzoReaderSettings());

        await loader.LoadAsync(new BaseArazzoReference { ExternalResource = "/" }, mainDocument, TestContext.Current.CancellationToken);

        var rootSchema = workspace.ResolveReference<IArazzoInput>("https://example.com/root/external-schema.json");
        var sharedSchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-schema.json#/$defs/shared");
        var propertyReference = Assert.IsType<ArazzoInputReference>(
            workspace.ResolveJsonSchemaReference("https://example.com/root/external-schema.json#/properties/value"));
        var propertySchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-schema.json#/properties/count");

        Assert.NotNull(rootSchema);
        Assert.NotNull(sharedSchema);
        Assert.Same(sharedSchema, propertyReference.Target);
        Assert.Equal(JsonSchemaType.Integer, propertySchema?.Type);
        Assert.Same(sharedSchema, ((ArazzoInputReference)mainDocument.Workflows[0].Inputs!).Target);
    }

    [Fact]
    public async Task LoadAsync_ExternalOpenApiDocument_RegistersSchemasAndResolvedSubSchemas()
    {
        const string externalOpenApiDocument = """
            {
              "openapi": "3.1.0",
              "info": {
                "title": "External",
                "version": "1.0.0"
              },
              "paths": {},
              "components": {
                "schemas": {
                  "Shared": {
                    "type": "string"
                  },
                  "Root": {
                    "type": "object",
                    "properties": {
                      "value": {
                        "$ref": "#/components/schemas/Shared"
                      },
                      "count": {
                        "type": "integer"
                      }
                    }
                  }
                }
              }
            }
            """;

        var mainDocument = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Workflows =
            [
                new ArazzoWorkflow
                {
                    WorkflowId = "wf",
                    Inputs = CreateExternalSchemaReference("Root", "external-openapi.json", "#/components/schemas/Root")
                }
            ]
        };
        ((ArazzoInputReference)mainDocument.Workflows[0].Inputs!).Reference.EnsureHostDocumentIsSet(mainDocument);

        var workspace = new ArazzoWorkspace(mainDocument.BaseUri);
        mainDocument.Workspace = workspace;
        var loader = new ArazzoWorkspaceLoader(
            workspace,
            new TestStreamLoader(new Dictionary<string, string>
            {
                ["https://example.com/root/external-openapi.json"] = externalOpenApiDocument
            }),
            new ArazzoReaderSettings());

        await loader.LoadAsync(new BaseArazzoReference { ExternalResource = "/" }, mainDocument, TestContext.Current.CancellationToken);

        var rootSchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.json#/components/schemas/Root");
        var sharedSchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.json#/components/schemas/Shared");
        var propertySchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.json#/components/schemas/Root/properties/count");

        Assert.NotNull(rootSchema);
        Assert.NotNull(sharedSchema);
        Assert.NotNull(propertySchema);
        var propertyReference = Assert.IsType<ArazzoInputReference>(
            workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.json#/components/schemas/Root/properties/value"));
        Assert.Same(sharedSchema, propertyReference.Target);
        Assert.Equal(JsonSchemaType.Integer, propertySchema?.Type);
        Assert.Same(rootSchema, ((ArazzoInputReference)mainDocument.Workflows[0].Inputs!).Target);
    }

    [Fact]
    public async Task LoadAsync_ExternalOpenApiYamlDocument_RegistersSchemasAndResolvedSubSchemas()
    {
        const string externalOpenApiDocument = """
            openapi: 3.1.0
            info:
              title: External
              version: 1.0.0
            paths: {}
            components:
              schemas:
                Shared:
                  type: string
                Root:
                  type: object
                  properties:
                    value:
                      $ref: '#/components/schemas/Shared'
                    count:
                      type: integer
            """;

        var mainDocument = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Workflows =
            [
                new ArazzoWorkflow
                {
                    WorkflowId = "wf",
                    Inputs = CreateExternalSchemaReference("Root", "external-openapi.yaml", "#/components/schemas/Root")
                }
            ]
        };
        ((ArazzoInputReference)mainDocument.Workflows[0].Inputs!).Reference.EnsureHostDocumentIsSet(mainDocument);

        var workspace = new ArazzoWorkspace(mainDocument.BaseUri);
        mainDocument.Workspace = workspace;
        var loader = new ArazzoWorkspaceLoader(
            workspace,
            new TestStreamLoader(new Dictionary<string, string>
            {
                ["https://example.com/root/external-openapi.yaml"] = externalOpenApiDocument
            }),
            new ArazzoReaderSettings());

        await loader.LoadAsync(new BaseArazzoReference { ExternalResource = "/" }, mainDocument, TestContext.Current.CancellationToken);

        var rootSchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.yaml#/components/schemas/Root");
        var sharedSchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.yaml#/components/schemas/Shared");
        var propertySchema = workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.yaml#/components/schemas/Root/properties/count");

        Assert.NotNull(rootSchema);
        Assert.NotNull(sharedSchema);
        Assert.NotNull(propertySchema);
        var propertyReference = Assert.IsType<ArazzoInputReference>(
            workspace.ResolveJsonSchemaReference("https://example.com/root/external-openapi.yaml#/components/schemas/Root/properties/value"));
        Assert.Same(sharedSchema, propertyReference.Target);
        Assert.Equal(JsonSchemaType.Integer, propertySchema?.Type);
        Assert.Same(rootSchema, ((ArazzoInputReference)mainDocument.Workflows[0].Inputs!).Target);
    }

    private static IArazzoInput CreateNestedExternalReferenceGraph(ArazzoDocument document)
    {
        return new ArazzoInput
        {
            Definitions = new Dictionary<string, IArazzoInput>
            {
                ["def"] = CreateExternalReference(document)
            },
            AllOf = [CreateExternalReference(document)],
            OneOf = [CreateExternalReference(document)],
            AnyOf = [CreateExternalReference(document)],
            Not = CreateExternalReference(document),
            Items = CreateExternalReference(document),
            Properties = new Dictionary<string, IArazzoInput>
            {
                ["property"] = CreateExternalReference(document)
            },
            PatternProperties = new Dictionary<string, IArazzoInput>
            {
                ["^x-"] = CreateExternalReference(document)
            },
            AdditionalProperties = CreateExternalReference(document),
            UnevaluatedPropertiesSchema = CreateExternalReference(document)
        };
    }

    private static ArazzoInputReference CreateExternalReference(ArazzoDocument document)
    {
        var reference = new ArazzoInputReference("shared", document, "external.json");
        reference.Reference.SetJsonPointerPath("https://example.com/root/external.json#/components/inputs/shared", "#");
        return reference;
    }

    private static ArazzoInputReference CreateExternalSchemaReference(string referenceId, string externalResource, string pointer)
    {
        var reference = new ArazzoInputReference(referenceId, null, externalResource);
        reference.Reference.SetJsonPointerPath(pointer, "#");
        return reference;
    }

    private sealed class TestStreamLoader(IDictionary<string, string> documents) : IStreamLoader
    {
        public Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
        {
            var resolvedUri = new Uri(baseUrl, uri).AbsoluteUri;
            return Task.FromResult(CreateStream(documents[resolvedUri]));
        }

        private static Stream CreateStream(string document)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(document));
        }
    }
}