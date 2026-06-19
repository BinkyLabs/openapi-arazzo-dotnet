using System.Text;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoWorkspaceLoaderTests
{
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