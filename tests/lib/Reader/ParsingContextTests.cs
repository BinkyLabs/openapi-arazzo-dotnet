// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ParsingContextTests
{
    private static ParsingContext CreateContext() => new(new ArazzoDiagnostic());

    [Fact]
    public void GetFromTempStorage_NoScope_ReturnsDefaultWhenMissing()
    {
        var ctx = CreateContext();

        Assert.Null(ctx.GetFromTempStorage<string>("missing"));
    }

    [Fact]
    public void SetTempStorage_NoScope_StoresAndRetrieves()
    {
        var ctx = CreateContext();
        ctx.SetTempStorage("key", "value");

        Assert.Equal("value", ctx.GetFromTempStorage<string>("key"));
    }

    [Fact]
    public void SetTempStorage_NoScopeNullValue_RemovesKey()
    {
        var ctx = CreateContext();
        ctx.SetTempStorage("key", "value");
        ctx.SetTempStorage("key", null);

        Assert.Null(ctx.GetFromTempStorage<string>("key"));
    }

    [Fact]
    public void SetTempStorage_WithScope_StoresAndRetrieves()
    {
        var ctx = CreateContext();
        var scope = new object();
        ctx.SetTempStorage("key", "value", scope);

        Assert.Equal("value", ctx.GetFromTempStorage<string>("key", scope));
    }

    [Fact]
    public void GetFromTempStorage_UnknownScope_ReturnsDefault()
    {
        var ctx = CreateContext();
        var scope = new object();

        Assert.Null(ctx.GetFromTempStorage<string>("key", scope));
    }

    [Fact]
    public void SetTempStorage_WithScopeNullValue_RemovesKey()
    {
        var ctx = CreateContext();
        var scope = new object();
        ctx.SetTempStorage("key", "value", scope);
        ctx.SetTempStorage("key", null, scope);

        Assert.Null(ctx.GetFromTempStorage<string>("key", scope));
    }

    [Fact]
    public void StartObject_EndObject_AffectsLocation()
    {
        var ctx = CreateContext();
        Assert.Equal("#/", ctx.GetLocation());
        ctx.StartObject("foo");
        ctx.StartObject("bar/baz~qux");
        Assert.Equal("#/foo/bar~1baz~0qux", ctx.GetLocation());
        ctx.EndObject();
        Assert.Equal("#/foo", ctx.GetLocation());
        ctx.EndObject();
        Assert.Equal("#/", ctx.GetLocation());
    }

    [Fact]
    public void PushLoop_AddsKey_ReturnsTrue()
    {
        var ctx = CreateContext();
        Assert.True(ctx.PushLoop("loop1", "a"));
        Assert.True(ctx.PushLoop("loop1", "b"));
    }

    [Fact]
    public void PushLoop_DuplicateKey_ReturnsFalse()
    {
        var ctx = CreateContext();
        ctx.PushLoop("loop1", "a");
        Assert.False(ctx.PushLoop("loop1", "a"));
    }

    [Fact]
    public void PopLoop_RemovesTop()
    {
        var ctx = CreateContext();
        ctx.PushLoop("loop1", "a");
        ctx.PushLoop("loop1", "b");
        ctx.PopLoop("loop1");
        Assert.True(ctx.PushLoop("loop1", "b"));
    }

    [Fact]
    public void PopLoop_EmptyStack_DoesNothing()
    {
        var ctx = CreateContext();
        ctx.PushLoop("loop1", "a");
        ctx.PopLoop("loop1");
        ctx.PopLoop("loop1"); // no throw
    }

    [Fact]
    public void Parse_ValidArazzoDocument_ReturnsDocument()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ]
                }
              ]
            }
            """)!;

        var doc = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.NotNull(doc);
        Assert.NotNull(doc.Info);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, ctx.Diagnostic.SpecificationVersion);
    }

    [Fact]
    public void Parse_ArazzoVersion101_ReturnsDocument()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.1",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ]
                }
              ]
            }
            """)!;

        var doc = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.NotNull(doc);
        Assert.NotNull(doc.Info);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, ctx.Diagnostic.SpecificationVersion);
    }

    [Fact]
    public void Parse_MissingInfo_AddsDiagnosticError()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("Info is a REQUIRED field"));
    }

    [Fact]
    public void Parse_WorkflowParametersWithSameNameDifferentIn_DoesNotAddDiagnosticError()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1"
                    }
                  ],
                  "parameters": [
                    { "name": "token", "in": "header", "value": "one" },
                    { "name": "token", "in": "query", "value": "two" }
                  ]
                }
              ]
            }
            """)!;

        var document = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.NotNull(document);
        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("duplicate parameter", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "child" }], "parameters": [{ "name": "input", "value": "one" }, { "name": "input", "value": "two" }] }] }""", "duplicate parameter 'input' in '<unspecified>'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }], "parameters": [{ "name": "id", "value": "one" }] }] }""", "parameter 'id' must specify 'in' when applied to an operation step")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "child", "parameters": [{ "name": "input", "value": "one" }, { "name": "input", "value": "two" }] }] }] }""", "duplicate parameter 'input' in '<unspecified>'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "parameters": [{ "name": "id", "value": "one" }] }] }] }""", "parameter 'id' must specify 'in' when applied to an operation step")]
    public void Parse_ParameterValidationViolations_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WorkflowTargetParametersWithoutIn_DoesNotAddDiagnosticError()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1",
                      "workflowId": "child",
                      "parameters": [
                        { "name": "input", "value": "step" }
                      ]
                    }
                  ],
                  "parameters": [
                    { "name": "workflowInput", "value": "workflow" }
                  ]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("parameter", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "Info.Title is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "Info.Version is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "SourceDescriptions is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "SourceDescriptions is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }] }""", "Workflows is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [] }""", "Workflows is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf" }] }""", "steps is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [] }] }""", "steps is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoSourceDescription.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoSourceDescription.Url is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "steps": [{ "stepId": "step1" }] }] }""", "ArazzoWorkflow.WorkflowId is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "operationId": "getPet" }] }] }""", "ArazzoStep.StepId is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "parameters": [{ "value": "1" }], "steps": [{ "stepId": "step1", "operationId": "getPet" }] }] }""", "ArazzoParameter.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "parameters": [{ "name": "id" }], "steps": [{ "stepId": "step1", "operationId": "getPet" }] }] }""", "ArazzoParameter.Value is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getPet" }], "successActions": [{ "type": "goto", "stepId": "step1" }] }] }""", "ArazzoSuccessAction.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getPet" }], "successActions": [{ "name": "goto", "stepId": "step1" }] }] }""", "ArazzoSuccessAction.Type is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getPet" }], "failureActions": [{ "type": "retry", "stepId": "step1" }] }] }""", "ArazzoFailureAction.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getPet" }], "failureActions": [{ "name": "retry", "stepId": "step1" }] }] }""", "ArazzoFailureAction.Type is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getPet", "requestBody": { "replacements": [{ "value": "updated" }] } }] }] }""", "ArazzoPayloadReplacement.Target is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getPet", "requestBody": { "replacements": [{ "target": "/name" }] } }] }] }""", "ArazzoPayloadReplacement.Value is a REQUIRED field")]
    public void Parse_MissingRequiredFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api1" }, { "name": "source1", "url": "https://example.com/api2" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "duplicate name 'source1'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }, { "workflowId": "wf", "steps": [{ "stepId": "step2" }] }] }""", "duplicate workflowId 'wf'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }, { "stepId": "step1" }] }] }""", "duplicate stepId 'step1'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "operationPath": "$sourceDescriptions.source1.url#/paths/~1users/get" }] }] }""", "can define only one of operationId, operationPath, or workflowId")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }], "successActions": [{ "name": "goto", "type": "goto", "workflowId": "next", "stepId": "step2" }] }] }""", "can define only one of workflowId or stepId")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "onFailure": [{ "name": "retry", "type": "retry", "workflowId": "refresh", "stepId": "retryStep" }] }] }] }""", "can define only one of workflowId or stepId")]
    public void Parse_UniquenessAndMutualExclusionViolations_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "missingWorkflow" }] }] }""", "references unknown workflowId 'missingWorkflow'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }], "successActions": [{ "name": "goto", "type": "goto", "stepId": "missingStep" }] }] }""", "references unknown stepId 'missingStep'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "parameters": [{ "reference": "$components.parameters.missing" }] }] }] }""", "reference '$components.parameters.missing' does not resolve")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationPath": "{$sourceDescriptions.missing.url}#/paths/~1users/get" }] }] }""", "references unknown sourceDescription 'missing'")]
    public void Parse_UnresolvedSemanticReferences_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_ResolvedSemanticReferences_DoesNotAddDiagnosticError()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.1",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1",
                      "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users/get",
                      "parameters": [{ "reference": "$components.parameters.shared" }]
                    }
                  ],
                  "successActions": [{ "name": "goto", "type": "goto", "stepId": "step1" }]
                },
                {
                  "workflowId": "child",
                  "steps": [{ "stepId": "childStep" }]
                }
              ],
              "components": {
                "parameters": {
                  "shared": { "name": "id", "in": "query", "value": "1" }
                }
              }
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("unknown", StringComparison.OrdinalIgnoreCase) || e.Message.Contains("does not resolve", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_MissingVersion_ThrowsOpenApiException()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            { "info": { "title": "T" } }
            """)!;

        Assert.Throws<OpenApiException>(() => ctx.Parse(jsonNode, new Uri("https://example.com/")));
    }

    [Fact]
    public void Parse_UnsupportedVersion_ThrowsUnsupportedSpecException()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            { "arazzo": "9.9.9", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [] }
            """)!;

        Assert.Throws<OpenApiUnsupportedSpecVersionException>(() => ctx.Parse(jsonNode, new Uri("https://example.com/")));
    }

    [Fact]
    public void ParseFragment_Arazzo1_0_ReturnsElement()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""{ "title": "T", "version": "1" }""")!;

        var info = ctx.ParseFragment<ArazzoInfo>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.NotNull(info);
        Assert.Equal("T", info!.Title);
    }

    [Fact]
    public void ParseFragment_UnsupportedVersion_Throws()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""{ "title": "T" }""")!;

        Assert.Throws<OpenApiUnsupportedSpecVersionException>(() => ctx.ParseFragment<ArazzoInfo>(jsonNode, (ArazzoSpecVersion)999));
    }
}