using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoWorkspaceLoaderTests
{
    [Theory]
    [InlineData("https://example.com/canonical/arazzo.json")]
    [InlineData("https://EXAMPLE.COM:443/canonical/./arazzo.json")]
    [InlineData("https://EXAMPLE.COM:443/root/./arazzo.json")]
    public async Task LoadAsync_ReferencesToParsedDocumentIdentitiesDoNotFetchAgain(string referenceUrl)
    {
        var json = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(json);
        json["$self"] = "https://example.com/canonical/arazzo.json";
        json["components"] = JsonNode.Parse("""{"inputs":{"shared":{"type":"string"}}}""");
        FirstObject(json, "workflows")["inputs"] = new JsonObject
        {
            ["$ref"] = $"{referenceUrl}#/components/inputs/shared"
        };
        var loader = new TestStreamLoader(new Dictionary<string, string>());

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInputReference>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.Target);
        Assert.Equal(JsonSchemaType.String, input.Target.Type);
        Assert.Equal(0, loader.LoadCount);
    }

    [Theory]
    [InlineData("https://other.example.com/canonical/external.json")]
    [InlineData("https://OTHER.EXAMPLE.COM:443/canonical/./external.json")]
    public async Task LoadAsync_RetrievalAndCanonicalReferencesReuseExternalDocumentComponents(string canonicalUrl)
    {
        var json = CreateExternalWorkflowDocument("external.json");
        FirstObject(json, "workflows")["inputs"] = new JsonObject
        {
            ["allOf"] = new JsonArray(
                new JsonObject { ["$ref"] = "external.json#/components/inputs/shared" },
                new JsonObject { ["$ref"] = $"{canonicalUrl}#/components/inputs/shared" })
        };
        var external = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(external);
        external["$self"] = "https://other.example.com/canonical/external.json";
        external["components"] = JsonNode.Parse("""{"inputs":{"shared":{"type":"string"}}}""");
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/external.json"] = external.ToJsonString()
        });

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInput>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.AllOf);
        var retrieval = Assert.IsType<ArazzoInputReference>(input.AllOf[0]);
        var canonical = Assert.IsType<ArazzoInputReference>(input.AllOf[1]);
        Assert.NotNull(retrieval.Target);
        Assert.Same(retrieval.Target, canonical.Target);
        Assert.Equal(1, loader.LoadCount);
    }

    [Theory]
    [InlineData("step")]
    [InlineData("dependsOn")]
    [InlineData("successActions")]
    [InlineData("failureActions")]
    [InlineData("onSuccess")]
    [InlineData("onFailure")]
    [InlineData("reusableAction")]
    public async Task LoadAsync_ReportsMissingExternalWorkflowTargets(string location)
    {
        var json = CreateExternalWorkflowDocument("external.json");
        var workflow = FirstObject(json, "workflows");
        var step = FirstObject(workflow, "steps");
        const string missing = "$sourceDescriptions.external.missing";
        switch (location)
        {
            case "step":
                step["workflowId"] = missing;
                break;
            case "dependsOn":
                Assert.IsType<JsonArray>(workflow["dependsOn"])[0] = missing;
                break;
            case "onSuccess":
            case "onFailure":
                FirstObject(step, location)["workflowId"] = missing;
                break;
            case "reusableAction":
                json["components"] = JsonNode.Parse("""
                    {"successActions":{"next":{"name":"next","type":"goto","workflowId":"$sourceDescriptions.external.missing"}}}
                    """);
                workflow["successActions"] = JsonNode.Parse("""[{"reference":"$components.successActions.next"}]""");
                break;
            default:
                FirstObject(workflow, location)["workflowId"] = missing;
                break;
        }
        Assert.IsType<JsonArray>(json["workflows"]).Add(JsonNode.Parse("""
            {"workflowId":"other","steps":[{"stepId":"call","workflowId":"$sourceDescriptions.external.target"}]}
            """));
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/external.json"] = ExternalWorkflowJson
        });

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Single(result.Diagnostic.Errors, error =>
            error.Message.Contains("does not resolve to workflowId 'missing'", StringComparison.Ordinal));
        Assert.Equal(1, loader.LoadCount);
        using var output = new StringWriter();
        var exception = Assert.Throws<ArazzoSerializationException>(() =>
            result.Document.SerializeAsV1_1(new OpenApiJsonWriter(output)));
        Assert.Contains("does not resolve to workflowId 'missing'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1.0.1", "external.json", ExternalWorkflowJson)]
    [InlineData("1.1.0", "https://example.com/root/external.yaml", """
        arazzo: 1.1.0
        info:
          title: External
          version: '1'
        sourceDescriptions:
          - name: api
            url: https://example.com/api.json
            type: openapi
        workflows:
          - workflowId: target
            steps:
              - stepId: operation
                operationId: op
        """)]
    public async Task LoadAsync_ResolvesExternalWorkflowsInJsonAndYaml(string version, string url, string external)
    {
        var json = CreateExternalWorkflowDocument(url);
        json["arazzo"] = version;
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            [new Uri(new Uri("https://example.com/root/arazzo.json"), url).AbsoluteUri] = external
        });

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(1, loader.LoadCount);
        var workspace = result.Document.Workspace;
        Assert.NotNull(workspace);
        var copy = new ArazzoWorkspace(workspace);
        Assert.True(copy.TryGetArazzoDocument(new Uri(new Uri("https://example.com/root/arazzo.json"), url), out var loaded));
        Assert.NotNull(loaded);
        Assert.Contains(loaded.Workflows ?? [], workflow => workflow.WorkflowId == "target");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadAsync_LoadsNestedSourcesOnceAndResolvesCyclesUsingSelfAndDocumentScope(bool missingNestedWorkflow)
    {
        var json = CreateExternalWorkflowDocument("external.json");
        var external = CreateExternalWorkflowDocument("https://example.com/root/arazzo.json");
        external["$self"] = "https://other.example.com/canonical/external.json";
        FirstObject(external, "workflows")["workflowId"] = "target";
        FirstObject(external, "sourceDescriptions")["url"] = "leaf.json";
        if (missingNestedWorkflow)
        {
            FirstObject(FirstObject(external, "workflows"), "steps")["workflowId"] = "$sourceDescriptions.external.missing";
        }
        var leaf = CreateExternalWorkflowDocument("https://example.com/root/arazzo.json");
        FirstObject(leaf, "workflows")["workflowId"] = "target";
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/external.json"] = external.ToJsonString(),
            ["https://other.example.com/canonical/leaf.json"] = leaf.ToJsonString()
                .Replace("$sourceDescriptions.external.target", "$sourceDescriptions.external.root", StringComparison.Ordinal)
        });

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        if (missingNestedWorkflow)
        {
            Assert.Contains(result.Diagnostic.Errors, error =>
                error.Message.Contains("Workflow 'target' step 'call'", StringComparison.Ordinal) &&
                error.Message.Contains("does not resolve to workflowId 'missing'", StringComparison.Ordinal));
        }
        else
        {
            Assert.Empty(result.Diagnostic.Errors);
        }
        Assert.Equal(2, loader.LoadCount);
    }

    [Fact]
    public async Task LoadAsync_ReportsUnavailableExternalArazzoSources()
    {
        var result = await LoadExternalWorkflowDocumentAsync(CreateExternalWorkflowDocument("external.json"),
            new TestStreamLoader(new Dictionary<string, string>()));

        Assert.NotNull(result.Diagnostic);
        Assert.Contains(result.Diagnostic.Errors, error =>
            error.Message.Contains("Arazzo sourceDescription 'external'", StringComparison.Ordinal) &&
            error.Message.Contains("could not be loaded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoadAsync_ArazzoSourceWithSelfAlsoResolvesReferencedInputComponents()
    {
        var json = CreateExternalWorkflowDocument("external.json");
        FirstObject(json, "workflows")["inputs"] = JsonNode.Parse("""{"$ref":"external.json#/components/inputs/shared"}""");
        var external = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(external);
        external["$self"] = "https://other.example.com/canonical/external.json";
        external["components"] = JsonNode.Parse("""{"inputs":{"shared":{"type":"string"}}}""");
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/external.json"] = external.ToJsonString()
        });

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInputReference>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.Target);
        Assert.Equal(JsonSchemaType.String, input.Target.Type);
        Assert.Equal(1, loader.LoadCount);
    }

    [Fact]
    public async Task LoadAsync_PropagatesExternalSourceCancellation()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            LoadExternalWorkflowDocumentAsync(CreateExternalWorkflowDocument("external.json"), new CancelingStreamLoader()));
    }

    [Theory]
    [InlineData("""{"openapi":"3.1.0","info":{"title":"Wrong type","version":"1"},"paths":{}}""")]
    [InlineData("""{"arazzo":"1.1.0","info":{"title":"Invalid","version":"1"},"sourceDescriptions":[],"workflows":[]}""")]
    [InlineData("not valid JSON or YAML: [")]
    public async Task LoadAsync_ReportsInvalidExternalArazzoSources(string external)
    {
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/external.json"] = external
        });

        var result = await LoadExternalWorkflowDocumentAsync(CreateExternalWorkflowDocument("external.json"), loader);

        Assert.NotNull(result.Diagnostic);
        Assert.Contains(result.Diagnostic.Errors, error =>
            error.Message.Contains("Arazzo sourceDescription 'external'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoadAsync_DoesNotFetchExternalWorkflowsWhenExternalLoadingIsDisabled()
    {
        var loader = new TestStreamLoader(new Dictionary<string, string>());

        var result = await LoadExternalWorkflowDocumentAsync(CreateExternalWorkflowDocument("external.json"), loader, false);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(0, loader.LoadCount);
    }

    private const string ExternalWorkflowJson = """
        {
          "arazzo": "1.1.0",
          "info": { "title": "External", "version": "1" },
          "sourceDescriptions": [{ "name": "api", "url": "https://example.com/api.json", "type": "openapi" }],
          "workflows": [{ "workflowId": "target", "steps": [{ "stepId": "operation", "operationId": "op" }] }]
        }
        """;

    private static JsonNode CreateExternalWorkflowDocument(string url)
    {
        var json = JsonNode.Parse("""
            {
              "arazzo": "1.1.0",
              "info": { "title": "Test", "version": "1" },
              "sourceDescriptions": [{ "name": "external", "url": "external.json", "type": "arazzo" }],
              "workflows": [{
                "workflowId": "root",
                "dependsOn": ["$sourceDescriptions.external.target"],
                "steps": [{
                  "stepId": "call",
                  "workflowId": "$sourceDescriptions.external.target",
                  "onSuccess": [{ "name": "next", "type": "goto", "workflowId": "$sourceDescriptions.external.target" }],
                  "onFailure": [{ "name": "retry", "type": "retry", "workflowId": "$sourceDescriptions.external.target" }]
                }],
                "successActions": [{ "name": "next", "type": "goto", "workflowId": "$sourceDescriptions.external.target" }],
                "failureActions": [{ "name": "retry", "type": "retry", "workflowId": "$sourceDescriptions.external.target" }]
              }]
            }
            """);
        Assert.NotNull(json);
        FirstObject(json, "sourceDescriptions")["url"] = url;
        return json;
    }

    private static JsonObject FirstObject(JsonNode node, string property)
    {
        return Assert.IsType<JsonObject>(Assert.IsType<JsonArray>(node[property])[0]);
    }

    private static async Task<ReadResult> LoadExternalWorkflowDocumentAsync(JsonNode json, IStreamLoader loader, bool loadExternalRefs = true)
    {
        var settings = new ArazzoReaderSettings();
        settings.OpenApiSettings.LoadExternalRefs = loadExternalRefs;
        settings.OpenApiSettings.CustomExternalLoader = loader;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.ToJsonString()));
        return await ArazzoModelFactory.LoadFromStreamAsync(stream, OpenApiConstants.Json, settings,
            TestContext.Current.CancellationToken, new Uri("https://example.com/root/arazzo.json"));
    }

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
        public int LoadCount { get; private set; }

        public Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            var resolvedUri = new Uri(baseUrl, uri).AbsoluteUri;
            return Task.FromResult(CreateStream(documents.TryGetValue(resolvedUri, out var document)
                ? document
                : throw new FileNotFoundException($"Document '{resolvedUri}' is not available.")));
        }

        private static Stream CreateStream(string document)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(document));
        }
    }

    private sealed class CancelingStreamLoader : IStreamLoader
    {
        public Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
        {
            return Task.FromCanceled<Stream>(new CancellationToken(true));
        }
    }
}