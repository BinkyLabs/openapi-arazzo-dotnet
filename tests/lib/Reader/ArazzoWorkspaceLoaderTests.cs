using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoWorkspaceLoaderTests
{
    private const string ReviewApiUrl = "https://api.example.test/spec.json";
    private const string ReviewOpenApiJson = """
        {"openapi":"3.2.0","info":{"title":"Test API","version":"1"},
         "paths":{"/op":{"get":{"operationId":"op","responses":{"200":{"description":"OK"}}}}}}
        """;

    private static JsonNode CreateOpenApiWorkflowDocument()
    {
        var json = JsonNode.Parse("""
            {"arazzo":"1.1.0","info":{"title":"Test","version":"1"},
             "sourceDescriptions":[{"name":"api","url":"https://api.example.test/spec.json","type":"openapi"}],
             "workflows":[{"workflowId":"wf","steps":[{"stepId":"operation","operationId":"op"}]}]}
            """);
        Assert.NotNull(json);
        return json;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoadAsync_OpenApiHttpSourcesRespectExternalLoadingOption(bool enabled)
    {
        var loader = new TestStreamLoader(new Dictionary<string, string> { [ReviewApiUrl] = ReviewOpenApiJson });
        var result = await LoadExternalWorkflowDocumentAsync(CreateOpenApiWorkflowDocument(), loader, enabled);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(enabled ? 1 : 0, loader.LoadCount);
    }

    [Theory]
    [InlineData(null, "could not be loaded")]
    [InlineData("{", "OpenAPI sourceDescription")]
    public async Task LoadAsync_ReportsOpenApiSourceFailures(string? content, string message)
    {
        var documents = new Dictionary<string, string>();
        if (content is not null)
            documents[ReviewApiUrl] = content;
        var result = await LoadExternalWorkflowDocumentAsync(CreateOpenApiWorkflowDocument(), new TestStreamLoader(documents));
        Assert.NotNull(result.Diagnostic);
        Assert.Contains(result.Diagnostic.Errors, error => error.Message.Contains(message, StringComparison.Ordinal) && error.Message.Contains(ReviewApiUrl, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoadAsync_DoesNotPassDeferredAsyncApiSourcesToOpenApiReader()
    {
        var json = CreateOpenApiWorkflowDocument();
        FirstObject(json, "sourceDescriptions")["type"] = "asyncapi";
        FirstObject(FirstObject(json, "workflows"), "steps")["action"] = "receive";
        var loader = new TestStreamLoader(new Dictionary<string, string>());
        var result = await LoadExternalWorkflowDocumentAsync(json, loader);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(0, loader.LoadCount);
    }

    [Fact]
    public async Task LoadAsync_OpenApiSourceCancellationPropagates()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            LoadExternalWorkflowDocumentAsync(CreateOpenApiWorkflowDocument(), new CancelingStreamLoader()));
    }

    [Theory]
    [InlineData("application/json", "{\"id\":1}", true, true)]
    [InlineData("application/json", "{bad}", false, true)]
    [InlineData("application/json", "{\"id\":1}", true, false)]
    [InlineData("application/json", "{bad}", false, false)]
    [InlineData("application/x-www-form-urlencoded", "id=a%20b", true, true)]
    [InlineData("application/x-www-form-urlencoded", "id=%ZZ", false, true)]
    public async Task LoadAsync_ValidatesInheritedQueryStringContent(string mediaType, string value, bool valid, bool useOperationPath)
    {
        var api = JsonNode.Parse(ReviewOpenApiJson);
        Assert.NotNull(api);
        var path = Assert.IsType<JsonObject>(api["paths"]?["/op"]);
        path["parameters"] = new JsonArray(new JsonObject
        {
            ["name"] = "query",
            ["in"] = "querystring",
            ["content"] = new JsonObject { [mediaType] = new JsonObject { ["schema"] = new JsonObject { ["type"] = "object" } } }
        });
        var json = CreateOpenApiWorkflowDocument();
        var workflow = FirstObject(json, "workflows");
        workflow["parameters"] = new JsonArray(new JsonObject { ["name"] = "query", ["in"] = "querystring", ["value"] = value });
        var step = FirstObject(workflow, "steps");
        if (useOperationPath)
        {
            step.Remove("operationId");
            step["operationPath"] = "{$sourceDescriptions.api.url}#/paths/~1op/get";
        }
        else
            step["operationId"] = "$sourceDescriptions.api.op";
        var loader = new TestStreamLoader(new Dictionary<string, string> { [ReviewApiUrl] = api.ToJsonString() });
        var result = await LoadExternalWorkflowDocumentAsync(json, loader);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        if (valid)
        {
            Assert.Empty(result.Diagnostic.Errors);
            using var text = new StringWriter();
            result.Document.SerializeAsV1_1(new OpenApiJsonWriter(text));
        }
        else
        {
            Assert.Contains(result.Diagnostic.Errors, error => error.Message.Contains("querystring parameter 'query' must contain valid", StringComparison.Ordinal));
            using var text = new StringWriter();
            Assert.Throws<ArazzoSerializationException>(() => result.Document.SerializeAsV1_1(new OpenApiJsonWriter(text)));
        }
    }

    [Fact]
    public async Task LoadAsync_QueryStringOverridesAndRuntimeValuesRemainValid()
    {
        var api = JsonNode.Parse(ReviewOpenApiJson);
        Assert.NotNull(api);
        var operation = Assert.IsType<JsonObject>(api["paths"]?["/op"]?["get"]);
        operation["parameters"] = JsonNode.Parse("""
            [{"name":"query","in":"querystring","content":{"application/json":{"schema":{"type":"object"}}}}]
            """);
        var json = CreateOpenApiWorkflowDocument();
        json["components"] = JsonNode.Parse("""
            {"parameters":{"inherited":{"name":"query","in":"querystring","value":"{bad}"}}}
            """);
        var workflow = FirstObject(json, "workflows");
        workflow["parameters"] = JsonNode.Parse("""[{"reference":"$components.parameters.inherited","value":"{bad}"}]""");
        FirstObject(workflow, "steps")["parameters"] = JsonNode.Parse("""
            [{"name":"query","in":"querystring","value":"{$inputs.query}"}]
            """);
        var result = await LoadExternalWorkflowDocumentAsync(json,
            new TestStreamLoader(new Dictionary<string, string> { [ReviewApiUrl] = api.ToJsonString() }));
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        using var text = new StringWriter();
        result.Document.SerializeAsV1_1(new OpenApiJsonWriter(text));
    }

    [Theory]
    [InlineData("target", "missing", "unknown stepId")]
    [InlineData("missing", "operation", "unknown workflowId")]
    public async Task LoadAsync_RejectsMissingExternalDependencyTargets(string workflowId, string stepId, string message)
    {
        var json = CreateExternalWorkflowDocument("external.json");
        FirstObject(FirstObject(json, "workflows"), "steps")["dependsOn"] =
            new JsonArray($"$sourceDescriptions.external.{workflowId}.steps.{stepId}");
        var loader = new TestStreamLoader(new Dictionary<string, string> { ["https://example.com/root/external.json"] = ExternalWorkflowJson });
        var result = await LoadExternalWorkflowDocumentAsync(json, loader);
        Assert.NotNull(result.Diagnostic);
        Assert.Contains(result.Diagnostic.Errors, error => error.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoadAsync_DetectsCrossDocumentStepDependencyCycles()
    {
        var json = CreateExternalWorkflowDocument("external.json");
        FirstObject(FirstObject(json, "workflows"), "steps")["dependsOn"] =
            new JsonArray("$sourceDescriptions.external.target.steps.operation");
        var external = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(external);
        Assert.IsType<JsonArray>(external["sourceDescriptions"]).Add(new JsonObject
        {
            ["name"] = "parent",
            ["url"] = "https://example.com/root/arazzo.json",
            ["type"] = "arazzo"
        });
        FirstObject(FirstObject(external, "workflows"), "steps")["dependsOn"] =
            new JsonArray("$sourceDescriptions.parent.root.steps.call");
        var loader = new TestStreamLoader(new Dictionary<string, string> { ["https://example.com/root/external.json"] = external.ToJsonString() });
        var result = await LoadExternalWorkflowDocumentAsync(json, loader);
        Assert.NotNull(result.Diagnostic);
        Assert.Contains(result.Diagnostic.Errors, error => error.Message.Contains("Step dependency cycle detected", StringComparison.Ordinal) &&
            error.Message.Contains("external.json", StringComparison.Ordinal) && error.Message.Contains("arazzo.json", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("#shared")]
    [InlineData("#%73hared")]
    public async Task LoadAsync_ResolvesPlainNameAnchorsLikeJsonPointers(string anchorReference)
    {
        var json = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(json);
        json["$self"] = "https://example.com/canonical/arazzo.json";
        var schema = JsonNode.Parse("""
            {
              "$anchor": "node",
              "$defs": { "shared": { "$anchor": "shared", "type": "string" } },
              "properties": {
                "anchor": { "$ref": "#shared" },
                "pointer": { "$ref": "#/workflows/0/inputs/$defs/shared" },
                "recursive": { "$ref": "#node" }
              }
            }
            """);
        Assert.NotNull(schema);
        Assert.IsType<JsonObject>(schema["properties"])["anchor"] = new JsonObject { ["$ref"] = anchorReference };
        FirstObject(json, "workflows")["inputs"] = schema;
        var loader = new TestStreamLoader(new Dictionary<string, string>());

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInput>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.Definitions);
        Assert.NotNull(input.Properties);
        var anchor = Assert.IsType<ArazzoInputReference>(input.Properties["anchor"]);
        var pointer = Assert.IsType<ArazzoInputReference>(input.Properties["pointer"]);
        var recursive = Assert.IsType<ArazzoInputReference>(input.Properties["recursive"]);
        Assert.Same(input.Definitions["shared"], anchor.Target);
        Assert.Same(pointer.Target, anchor.Target);
        Assert.Same(input, recursive.Target);
        Assert.Equal(1, loader.LoadCount);

        using var output = new StringWriter();
        result.Document.SerializeAsV1_1(new OpenApiJsonWriter(output));
        var roundTrip = await ArazzoModelFactory.ParseAsync(output.ToString(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(roundTrip.Document);
        Assert.NotNull(roundTrip.Diagnostic);
        Assert.Empty(roundTrip.Diagnostic.Errors);
        Assert.NotNull(roundTrip.Document.Workflows);
        var roundTripInput = Assert.IsType<ArazzoInput>(roundTrip.Document.Workflows[0].Inputs);
        Assert.NotNull(roundTripInput.Definitions);
        Assert.NotNull(roundTripInput.Properties);
        Assert.Same(roundTripInput.Definitions["shared"], Assert.IsType<ArazzoInputReference>(roundTripInput.Properties["anchor"]).Target);
    }

    [Theory]
    [InlineData("contains")]
    [InlineData("if")]
    [InlineData("dependentSchemas")]
    public async Task LoadAsync_RegistersAnchorsInOtherSubschemaKeywords(string keyword)
    {
        var json = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(json);
        var anchored = new JsonObject { ["$anchor"] = "shared", ["type"] = "string" };
        FirstObject(json, "workflows")["inputs"] = new JsonObject
        {
            [keyword] = keyword == "dependentSchemas" ? new JsonObject { ["trigger"] = anchored } : anchored,
            ["allOf"] = new JsonArray(new JsonObject { ["$ref"] = "#shared" })
        };

        var result = await LoadExternalWorkflowDocumentAsync(json, new TestStreamLoader(new Dictionary<string, string>()));

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInput>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.AllOf);
        var reference = Assert.IsType<ArazzoInputReference>(input.AllOf[0]);
        Assert.NotNull(reference.Target);
        Assert.Equal(JsonSchemaType.String, reference.Target.Type);
    }

    [Fact]
    public async Task LoadAsync_ResolvesAnchorsWithinNestedSchemaIdScopes()
    {
        var json = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(json);
        FirstObject(json, "workflows")["inputs"] = JsonNode.Parse("""
            {
              "$id": "schemas/root.json",
              "$defs": {
                "shared": { "$anchor": "shared", "type": "string" },
                "nested": {
                  "$id": "nested/inner.json",
                  "$defs": { "shared": { "$anchor": "shared", "type": "integer" } },
                  "properties": {
                    "anchor": { "$ref": "#shared" },
                    "pointer": { "$ref": "#/$defs/shared" },
                    "parent": { "$ref": "../root.json#shared" }
                  }
                }
              },
              "properties": {
                "anchor": { "$ref": "#shared" },
                "nested": { "$ref": "nested/inner.json#shared" },
                "root": { "$ref": "#" },
                "unknown": { "$ref": "#Shared" }
              }
            }
            """);
        var loader = new TestStreamLoader(new Dictionary<string, string>());

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInput>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.Definitions);
        Assert.NotNull(input.Properties);
        var nested = Assert.IsType<ArazzoInput>(input.Definitions["nested"]);
        Assert.NotNull(nested.Definitions);
        Assert.NotNull(nested.Properties);
        Assert.Same(input.Definitions["shared"], Assert.IsType<ArazzoInputReference>(input.Properties["anchor"]).Target);
        Assert.Same(nested.Definitions["shared"], Assert.IsType<ArazzoInputReference>(input.Properties["nested"]).Target);
        Assert.Same(nested.Definitions["shared"], Assert.IsType<ArazzoInputReference>(nested.Properties["anchor"]).Target);
        var pointer = Assert.IsType<ArazzoInputReference>(nested.Properties["pointer"]);
        Assert.NotNull(pointer.Target);
        Assert.Same(nested.Definitions["shared"], pointer.Target);
        Assert.Same(input.Definitions["shared"], Assert.IsType<ArazzoInputReference>(nested.Properties["parent"]).Target);
        Assert.Same(input, Assert.IsType<ArazzoInputReference>(input.Properties["root"]).Target);
        Assert.Null(Assert.IsType<ArazzoInputReference>(input.Properties["unknown"]).Target);
        Assert.Equal(1, loader.LoadCount);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("https://schemas.example.com/canonical.json", false)]
    [InlineData("https://schemas.example.com/canonical.json", true)]
    public async Task LoadAsync_ResolvesAnchorsInExternalJsonAndYamlSchemaResources(string? id, bool yaml)
    {
        var json = JsonNode.Parse(ExternalWorkflowJson);
        Assert.NotNull(json);
        FirstObject(json, "workflows")["inputs"] = JsonNode.Parse("""{"$ref":"schema.json#shared"}""");
        var schema = JsonNode.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$defs": {
                "shared": { "$anchor": "shared", "type": "string" },
                "nested": {
                  "$id": "nested/inner.json",
                  "$defs": { "shared": { "$anchor": "shared", "type": "integer" } },
                  "properties": { "pointer": { "$ref": "#/$defs/shared" } }
                }
              }
            }
            """);
        Assert.NotNull(schema);
        if (id is not null)
        {
            schema["$id"] = id;
        }
        var loader = new TestStreamLoader(new Dictionary<string, string>
        {
            ["https://example.com/root/schema.json"] = yaml ? $"""
                $schema: https://json-schema.org/draft/2020-12/schema
                $id: {id}
                $defs:
                  shared:
                    $anchor: shared
                    type: string
                  nested:
                    $id: nested/inner.json
                    $defs:
                      shared:
                        $anchor: shared
                        type: integer
                    properties:
                      pointer:
                        $ref: '#/$defs/shared'
                """ : schema.ToJsonString()
        });

        var result = await LoadExternalWorkflowDocumentAsync(json, loader);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic.Errors);
        Assert.NotNull(result.Document.Workflows);
        var input = Assert.IsType<ArazzoInputReference>(result.Document.Workflows[0].Inputs);
        Assert.NotNull(input.Target);
        Assert.Equal(JsonSchemaType.String, input.Target.Type);
        Assert.NotNull(result.Document.Workspace);
        var pointer = Assert.IsType<ArazzoInputReference>(result.Document.Workspace.ResolveJsonSchemaReference(
            "https://example.com/root/schema.json#/$defs/nested/properties/pointer"));
        Assert.NotNull(pointer.Target);
        Assert.Equal(JsonSchemaType.Integer, pointer.Target.Type);
        Assert.Equal(2, loader.LoadCount);
    }

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
        Assert.Equal(1, loader.LoadCount);
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
        Assert.Equal(2, loader.LoadCount);
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
        Assert.Equal(2, loader.LoadCount);
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
        Assert.Equal(2, loader.LoadCount);
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
        FirstObject(leaf, "workflows").Remove("dependsOn");
        var leafStep = FirstObject(FirstObject(leaf, "workflows"), "steps");
        leafStep.Remove("workflowId");
        leafStep["operationId"] = "op";
        Assert.IsType<JsonArray>(leaf["sourceDescriptions"]).Add(new JsonObject
        {
            ["name"] = "api",
            ["url"] = "https://example.com/api.json",
            ["type"] = "openapi"
        });
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
        Assert.Equal(3, loader.LoadCount);
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
        Assert.Equal(2, loader.LoadCount);
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

    private sealed class TestStreamLoader : IStreamLoader
    {
        private readonly Dictionary<string, string> _documents;

        public TestStreamLoader(IDictionary<string, string> documents)
        {
            _documents = new() { ["https://example.com/api.json"] = ReviewOpenApiJson };
            foreach (var (uri, document) in documents)
                _documents[uri] = document;
        }

        public int LoadCount { get; private set; }

        public Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            var resolvedUri = new Uri(baseUrl, uri).AbsoluteUri;
            return Task.FromResult(CreateStream(_documents.TryGetValue(resolvedUri, out var document)
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