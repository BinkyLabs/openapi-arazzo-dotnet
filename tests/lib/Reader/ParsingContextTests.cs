// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ParsingContextTests
{
    private static ParsingContext CreateContext() => new(new ArazzoDiagnostic());

    [Theory]
    [InlineData("asyncapi", """{"stepId": "step", "operationId": "receiveOrder"}""", "action is REQUIRED")]
    [InlineData("asyncapi", """{"stepId": "step", "operationId": "$sourceDescriptions.source.receiveOrder"}""", "action is REQUIRED")]
    [InlineData("openapi", """{"stepId": "step", "operationId": "getOrder", "action": "send"}""", "action can only be specified for AsyncAPI steps")]
    [InlineData(null, """{"stepId": "step", "operationId": "getOrder", "action": "receive"}""", "action can only be specified for AsyncAPI steps")]
    [InlineData("openapi", """{"stepId": "step", "operationId": "$sourceDescriptions.source.getOrder", "action": "receive", "correlationId": "id"}""", "action can only be specified for AsyncAPI steps")]
    [InlineData("asyncapi", """{"stepId": "step", "channelPath": "{$sourceDescriptions.missing.url}#/channels/orders", "action": "receive"}""", "unknown sourceDescription 'missing'")]
    [InlineData("openapi", """{"stepId": "step", "channelPath": "{$sourceDescriptions.source.url}#/channels/orders", "action": "send"}""", "channelPath must target an AsyncAPI sourceDescription")]
    [InlineData("arazzo", """{"stepId": "step", "channelPath": "{$sourceDescriptions.source.url}#/channels/orders", "action": "receive"}""", "channelPath must target an AsyncAPI sourceDescription")]
    public void ParseAndSerialize_AsyncApiSourceConstraints_ReportsError(string? sourceType, string stepJson, string expectedMessage)
    {
        var jsonNode = JsonNode.Parse("""
        {
            "arazzo": "1.1.0",
            "info": {"title": "T", "version": "1"},
            "sourceDescriptions": [{"name": "source", "url": "https://example.com/api"}],
            "workflows": [{"workflowId": "wf", "steps": []}]
        }
        """);
        Assert.NotNull(jsonNode);
        var source = Assert.IsType<JsonObject>(Assert.IsType<JsonArray>(jsonNode["sourceDescriptions"])[0]);
        if (sourceType is not null)
            source["type"] = sourceType;
        var workflow = Assert.IsType<JsonObject>(Assert.IsType<JsonArray>(jsonNode["workflows"])[0]);
        var stepNode = JsonNode.Parse(stepJson);
        Assert.NotNull(stepNode);
        workflow["steps"] = new JsonArray(stepNode);
        var context = CreateContext();

        var document = context.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
        using var textWriter = new StringWriter();
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1_1(new OpenApiJsonWriter(textWriter)));
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndSerialize_MixedSources_UsesTargetSourceType()
    {
        var jsonNode = JsonNode.Parse("""
        {
            "arazzo": "1.1.0",
            "info": {"title": "T", "version": "1"},
            "sourceDescriptions": [
                {"name": "api", "url": "https://example.com/api", "type": "openapi"},
                {"name": "events", "url": "https://example.com/events", "type": "asyncapi"}
            ],
            "workflows": [{
                "workflowId": "wf",
                "steps": [
                    {"stepId": "getOrder", "operationId": "$sourceDescriptions.api.getOrder"},
                    {"stepId": "receiveOrder", "operationId": "$sourceDescriptions.events.receiveOrder", "action": "receive", "correlationId": "$inputs.id"},
                    {"stepId": "sendOrder", "channelPath": "{$sourceDescriptions.events.url}#/channels/order~1created", "action": "send"},
                    {"stepId": "receiveChannel", "channelPath": "{$sourceDescriptions.events.url}#/channels/orders", "action": "receive"}
                ]
            }]
        }
        """);
        Assert.NotNull(jsonNode);
        var context = CreateContext();

        var document = context.Parse(jsonNode, new Uri("https://example.com/"));
        using var textWriter = new StringWriter();
        document.SerializeAsV1_1(new OpenApiJsonWriter(textWriter));

        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(jsonNode, JsonNode.Parse(textWriter.ToString())));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void ParseAndSerialize_ConflictingQueryParameters_ReportsError(int placement, bool useReferences)
    {
        var query = useReferences
            ? """{"reference": "$components.parameters.query"}"""
            : """{"name": "filter", "in": "query", "value": "active"}""";
        var queryString = useReferences
            ? """{"reference": "$components.parameters.queryString"}"""
            : """{"name": "fullQuery", "in": "querystring", "value": "sort=desc"}""";
        var workflowParameters = placement switch
        {
            0 => $"[{query}, {queryString}]",
            2 => $"[{query}]",
            3 => $"[{queryString}]",
            _ => "[]"
        };
        var stepParameters = placement switch
        {
            1 => $"[{query}, {queryString}]",
            2 => $"[{queryString}]",
            3 => $"[{query}]",
            _ => "[]"
        };
        var jsonNode = JsonNode.Parse($$"""
        {
            "arazzo": "1.1.0",
            "info": {"title": "T", "version": "1"},
            "sourceDescriptions": [{"name": "api", "url": "https://example.com/api", "type": "openapi"}],
            "workflows": [{
                "workflowId": "wf",
                "parameters": {{workflowParameters}},
                "steps": [{"stepId": "step", "operationId": "getUser", "parameters": {{stepParameters}}}]
            }],
            "components": {
                "parameters": {
                    "query": {"name": "filter", "in": "query", "value": "active"},
                    "queryString": {"name": "fullQuery", "in": "querystring", "value": "sort=desc"}
                }
            }
        }
        """);
        Assert.NotNull(jsonNode);
        var context = CreateContext();

        var document = context.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("'query' and 'querystring' parameters cannot coexist", StringComparison.Ordinal));
        Assert.NotNull(document.Workflows);
        var workflow = Assert.Single(document.Workflows);
        using var textWriter = new StringWriter();
        var exception = Assert.Throws<ArazzoSerializationException>(() => workflow.SerializeAsV1_1(new OpenApiJsonWriter(textWriter)));
        Assert.Contains("'query' and 'querystring' parameters cannot coexist", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, false, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, false, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, true, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, true, false)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, false, true)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, false, true)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, true, true)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, true, true)]
    public void ParseAndSerialize_InvalidActionParameters_ReportsError(ArazzoSpecVersion version, bool failure, bool duplicate)
    {
        var parameters = duplicate
            ? """[{"name": "input", "value": "one"}, {"name": "input", "value": "two"}]"""
            : """[{"name": "input", "in": "query", "value": "one"}]""";
        var fieldName = version is ArazzoSpecVersion.Arazzo1_0 ? "x-parameters" : "parameters";
        var jsonNode = JsonNode.Parse($$"""
        {"name": "goto", "type": "goto", "workflowId": "child", "{{fieldName}}": {{parameters}}}
        """);
        Assert.NotNull(jsonNode);
        var context = CreateContext();
        IArazzoResultAction? action = failure
            ? context.ParseFragment<ArazzoFailureAction>(jsonNode, version)
            : context.ParseFragment<ArazzoSuccessAction>(jsonNode, version);
        Assert.NotNull(action);
        var expectedMessage = duplicate ? "duplicate parameter 'input'" : "parameter 'input' must not specify 'in'";

        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        var exception = Assert.Throws<ArazzoSerializationException>(() =>
        {
            if (version is ArazzoSpecVersion.Arazzo1_0)
                action.SerializeAsV1(writer);
            else
                action.SerializeAsV1_1(writer);
        });
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void ParseAndSerialize_ResolvedActionParameters_ReportsError(bool failure, bool duplicate, bool reusableAction)
    {
        var parameters = duplicate
            ? """[{"reference": "$components.parameters.shared"}, {"name": "input", "value": "two"}]"""
            : """[{"reference": "$components.parameters.shared"}]""";
        var action = $$"""{"name": "goto", "type": "goto", "workflowId": "wf", "parameters": {{parameters}}}""";
        var actionKind = failure ? "failureActions" : "successActions";
        var actionList = reusableAction ? $$"""{"reference": "$components.{{actionKind}}.shared"}""" : action;
        var parameter = duplicate
            ? """{"name": "input", "value": "one"}"""
            : """{"name": "input", "in": "header", "value": "one"}""";
        var jsonNode = JsonNode.Parse($$"""
        {
            "arazzo": "1.1.0",
            "info": {"title": "T", "version": "1"},
            "sourceDescriptions": [{"name": "api", "url": "https://example.com/api", "type": "openapi"}],
            "workflows": [{
                "workflowId": "wf",
                "steps": [{"stepId": "step", "operationId": "getUser"}],
                "{{actionKind}}": [{{actionList}}]
            }],
            "components": {
                "parameters": {"shared": {{parameter}}},
                "{{actionKind}}": {"shared": {{action}}}
            }
        }
        """);
        Assert.NotNull(jsonNode);
        var context = CreateContext();

        var document = context.Parse(jsonNode, new Uri("https://example.com/"));

        var expectedMessage = duplicate ? "duplicate parameter 'input'" : "parameter 'input' must not specify 'in'";
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
        using var textWriter = new StringWriter();
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1_1(new OpenApiJsonWriter(textWriter)));
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ParseAndSerialize_MissingActionParameterReference_ReportsError(bool failure, bool reusableAction)
    {
        var action = """{"name":"goto","type":"goto","workflowId":"wf","parameters":[{"reference":"$components.parameters.missing"}]}""";
        var actionKind = failure ? "failureActions" : "successActions";
        var actionList = reusableAction ? $$"""{"reference":"$components.{{actionKind}}.shared"}""" : action;
        var input = JsonNode.Parse($$"""
        {
            "arazzo":"1.1.0",
            "info":{"title":"T","version":"1"},
            "sourceDescriptions":[{"name":"api","url":"https://example.com/api","type":"openapi"}],
            "workflows":[{
                "workflowId":"wf",
                "steps":[{"stepId":"step","operationId":"getUser"}],
                "{{actionKind}}":[{{actionList}}]
            }],
            "components":{
                "{{actionKind}}":{
                    "shared":{{action}}
                }
            }
        }
        """);
        Assert.NotNull(input);
        var context = CreateContext();

        var document = context.Parse(input, new Uri("https://example.com/"));

        const string message = "parameter reference '$components.parameters.missing' does not resolve";
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(message, StringComparison.Ordinal));
        using var text = new StringWriter();
        var exception = Assert.Throws<ArazzoSerializationException>(() => document.SerializeAsV1_1(new OpenApiJsonWriter(text)));
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

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
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "parameters": [{ "name": "X-Api-Key", "in": "header", "value": "one" }, { "name": "x-api-key", "in": "header", "value": "two" }] }] }] }""", "duplicate parameter 'x-api-key' in 'header'")]
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

    [Theory]
    [InlineData("query")]
    [InlineData("path")]
    [InlineData("cookie")]
    public void Parse_NonHeaderParametersDifferingOnlyByCase_DoesNotAddDuplicateDiagnosticError(string location)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse($$"""
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
                      "operationId": "getUser",
                      "parameters": [
                        { "name": "UserId", "in": "{{location}}", "value": "one" },
                        { "name": "userid", "in": "{{location}}", "value": "two" }
                      ]
                    }
                  ]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("duplicate parameter", StringComparison.Ordinal));
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
    [InlineData("""{ "arazzo": "1.0.0", "info": { "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoInfo.Title is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoInfo.Version is a REQUIRED field")]
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
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "operationId": "getPet" }] }] }""", "ArazzoStep.StepId is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "", "operationId": "getPet" }] }] }""", "ArazzoStep.StepId is a REQUIRED field")]
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
    [InlineData("""{ "value": "updated" }""", "ArazzoPayloadReplacement.Target is a REQUIRED field")]
    [InlineData("""{ "target": "/name" }""", "ArazzoPayloadReplacement.Value is a REQUIRED field")]
    public void Parse_PayloadReplacementMissingRequiredFields_AddsSingleDiagnosticError(string replacementJson, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse($$"""
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
                      "operationId": "getPet",
                      "requestBody": {
                        "replacements": [
                          {{replacementJson}}
                        ]
                      }
                    }
                  ]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Single(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "steps": [{ "stepId": "step1", "operationId": "getPet" }] }] }""", "ArazzoWorkflow.WorkflowId is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "", "steps": [{ "stepId": "step1", "operationId": "getPet" }] }] }""", "ArazzoWorkflow.WorkflowId is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf" }] }""", "Workflow 'wf' steps is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [] }] }""", "Workflow 'wf' steps is a REQUIRED field")]
    public void Parse_MissingOrEmptyWorkflowRequiredFields_AddsDiagnosticErrorOnce(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Equal(1, ctx.Diagnostic.Errors.Count(e => e.Message.Contains(expectedMessage, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("""{ "operationId": "getPet" }""")]
    [InlineData("""{ "stepId": "", "operationId": "getPet" }""")]
    public void ParseFragment_MissingOrEmptyStepId_AddsDiagnosticError(string json)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoStep>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("ArazzoStep.StepId is a REQUIRED field", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "version": "1" }""", "ArazzoInfo.Title is a REQUIRED field")]
    [InlineData("""{ "title": "", "version": "1" }""", "ArazzoInfo.Title is a REQUIRED field")]
    [InlineData("""{ "title": "T" }""", "ArazzoInfo.Version is a REQUIRED field")]
    [InlineData("""{ "title": "T", "version": "" }""", "ArazzoInfo.Version is a REQUIRED field")]
    public void ParseFragment_MissingOrEmptyInfoRequiredFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoInfo>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoInfo.Title is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoInfo.Title is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoInfo.Version is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoInfo.Version is a REQUIRED field")]
    public void Parse_InfoRequiredFields_AddsSingleDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Equal(1, ctx.Diagnostic.Errors.Count(e => e.Message.Contains(expectedMessage, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("""{ "steps": [{ "stepId": "step1", "operationId": "getPet" }] }""", "ArazzoWorkflow.WorkflowId is a REQUIRED field")]
    [InlineData("""{ "workflowId": "", "steps": [{ "stepId": "step1", "operationId": "getPet" }] }""", "ArazzoWorkflow.WorkflowId is a REQUIRED field")]
    [InlineData("""{ "workflowId": "wf" }""", "Workflow 'wf' steps is a REQUIRED field")]
    [InlineData("""{ "workflowId": "wf", "steps": [] }""", "Workflow 'wf' steps is a REQUIRED field")]
    public void ParseFragment_MissingOrEmptyWorkflowRequiredFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoWorkflow>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "value": "1" }""", "ArazzoParameter.Name is a REQUIRED field")]
    [InlineData("""{ "name": "", "value": "1" }""", "ArazzoParameter.Name is a REQUIRED field")]
    [InlineData("""{ "name": "id" }""", "ArazzoParameter.Value is a REQUIRED field")]
    public void ParseFragment_MissingOrEmptyParameterRequiredFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoParameter>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "parameters": [{ "value": "1" }], "steps": [{ "stepId": "step1", "operationId": "getPet" }] }] }""", "ArazzoParameter.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "parameters": [{ "name": "id" }], "steps": [{ "stepId": "step1", "operationId": "getPet" }] }] }""", "ArazzoParameter.Value is a REQUIRED field")]
    public void Parse_MissingParameterRequiredFields_AddsSingleDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Equal(1, ctx.Diagnostic.Errors.Count(e => e.Message.Contains(expectedMessage, StringComparison.Ordinal)));
    }

    [Fact]
    public void Parse_ParameterReusableReference_DoesNotRequireNameOrValue()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(
            """
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }],
              "workflows": [{
                "workflowId": "wf",
                "steps": [{
                  "stepId": "step1",
                  "operationId": "getPet",
                  "parameters": [{ "reference": "$components.parameters.shared" }]
                }]
              }],
              "components": {
                "parameters": {
                  "shared": { "name": "id", "in": "query", "value": "1" }
                }
              }
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("ArazzoParameter.Name is a REQUIRED field", StringComparison.Ordinal));
        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("ArazzoParameter.Value is a REQUIRED field", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "type": "goto", "stepId": "step1" }""", "ArazzoSuccessAction.Name is a REQUIRED field")]
    [InlineData("""{ "name": "", "type": "goto", "stepId": "step1" }""", "ArazzoSuccessAction.Name is a REQUIRED field")]
    [InlineData("""{ "name": "goto", "stepId": "step1" }""", "ArazzoSuccessAction.Type is a REQUIRED field")]
    public void ParseFragment_MissingSuccessActionRequiredFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoSuccessAction>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "type": "retry" }""", "ArazzoFailureAction.Name is a REQUIRED field")]
    [InlineData("""{ "name": "", "type": "retry" }""", "ArazzoFailureAction.Name is a REQUIRED field")]
    [InlineData("""{ "name": "retry" }""", "ArazzoFailureAction.Type is a REQUIRED field")]
    public void ParseFragment_MissingFailureActionRequiredFields_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoFailureAction>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("successActions", """{ "type": "goto", "stepId": "step1" }""", "ArazzoSuccessAction.Name is a REQUIRED field")]
    [InlineData("successActions", """{ "name": "", "type": "goto", "stepId": "step1" }""", "ArazzoSuccessAction.Name is a REQUIRED field")]
    [InlineData("successActions", """{ "name": "goto", "stepId": "step1" }""", "ArazzoSuccessAction.Type is a REQUIRED field")]
    [InlineData("failureActions", """{ "type": "retry" }""", "ArazzoFailureAction.Name is a REQUIRED field")]
    [InlineData("failureActions", """{ "name": "", "type": "retry" }""", "ArazzoFailureAction.Name is a REQUIRED field")]
    [InlineData("failureActions", """{ "name": "retry" }""", "ArazzoFailureAction.Type is a REQUIRED field")]
    public void Parse_MissingActionRequiredFields_AddsSingleDiagnosticError(string actionCollectionName, string actionJson, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse($$"""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [{ "stepId": "step1", "operationId": "getPet" }],
                  "{{actionCollectionName}}": [{{actionJson}}]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));
        Assert.Single(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "url": "https://example.com/api" }""")]
    [InlineData("""{ "name": "", "url": "https://example.com/api" }""")]
    public void ParseFragment_MissingOrEmptySourceDescriptionName_AddsDiagnosticError(string json)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoSourceDescription>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("ArazzoSourceDescription.Name is a REQUIRED field", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "name": "source1" }""")]
    [InlineData("""{ "name": "source1", "url": null }""")]
    public void ParseFragment_MissingOrNullSourceDescriptionUrl_AddsDiagnosticError(string json)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.ParseFragment<ArazzoSourceDescription>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("ArazzoSourceDescription.Url is a REQUIRED field", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoSourceDescription.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoSourceDescription.Name is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoSourceDescription.Url is a REQUIRED field")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": null }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "ArazzoSourceDescription.Url is a REQUIRED field")]
    public void Parse_MissingSourceDescriptionRequiredFields_AddsOneDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Single(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api1" }, { "name": "source1", "url": "https://example.com/api2" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }] }] }""", "duplicate name 'source1'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }] }, { "workflowId": "wf", "steps": [{ "stepId": "step2", "operationId": "getUser" }] }] }""", "duplicate workflowId 'wf'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }, { "stepId": "step1", "operationId": "listUsers" }] }] }""", "duplicate stepId 'step1'")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users/get" }] }] }""", "can define only one of operationId, operationPath, or workflowId")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }] }] }""", "must define exactly one of operationId, operationPath, or workflowId")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "child", "requestBody": {} }] }] }""", "requestBody can only be specified when the step targets operationId or operationPath")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "requestBody": {} }] }] }""", "requestBody can only be specified when the step targets operationId or operationPath")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }], "successActions": [{ "name": "goto", "type": "goto", "workflowId": "next", "stepId": "step2" }] }] }""", "can define only one of workflowId or stepId")]
    [InlineData("""{ "arazzo": "1.0.0", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "onFailure": [{ "name": "retry", "type": "retry", "workflowId": "refresh", "stepId": "retryStep" }] }] }] }""", "can define only one of workflowId or stepId")]
    public void Parse_UniquenessAndMutualExclusionViolations_AddsDiagnosticError(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "parameters": [{ "$ref": "$components.parameters.shared" }] }] }], "components": { "parameters": { "shared": { "name": "id", "value": "1" } } } }""", "ArazzoParameter.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "onSuccess": [{ "$ref": "$components.successActions.shared" }] }] }], "components": { "successActions": { "shared": { "name": "done", "type": "end" } } } }""", "ArazzoSuccessAction.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser", "onFailure": [{ "$ref": "$components.failureActions.shared" }] }] }], "components": { "failureActions": { "shared": { "name": "retry", "type": "retry" } } } }""", "ArazzoFailureAction.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "parameters": [{ "$ref": "$components.parameters.shared" }], "steps": [{ "stepId": "step1", "operationId": "getUser" }] }], "components": { "parameters": { "shared": { "name": "id", "value": "1" } } } }""", "ArazzoParameter.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }], "successActions": [{ "$ref": "$components.successActions.shared" }] }], "components": { "successActions": { "shared": { "name": "done", "type": "end" } } } }""", "ArazzoSuccessAction.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }], "failureActions": [{ "$ref": "$components.failureActions.shared" }] }], "components": { "failureActions": { "shared": { "name": "retry", "type": "retry" } } } }""", "ArazzoFailureAction.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }] }], "components": { "parameters": { "shared": { "$ref": "$components.parameters.other" } } } }""", "ArazzoParameter.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }] }], "components": { "successActions": { "shared": { "$ref": "$components.successActions.other" } } } }""", "ArazzoSuccessAction.Name is a REQUIRED field.")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationId": "getUser" }] }], "components": { "failureActions": { "shared": { "$ref": "$components.failureActions.other" } } } }""", "ArazzoFailureAction.Name is a REQUIRED field.")]
    public void Parse_DollarRefReusableParameterOrAction_AddsRequiredFieldDiagnostic(string json, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse(json)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_ReusableReferenceKeyword_ResolvesStepAndWorkflowParameterAndActions()
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
                  "parameters": [{ "reference": "$components.parameters.shared" }],
                  "steps": [
                    {
                      "stepId": "step1",
                      "operationId": "getUser",
                      "parameters": [{ "reference": "$components.parameters.shared" }],
                      "onSuccess": [{ "reference": "$components.successActions.done" }],
                      "onFailure": [{ "reference": "$components.failureActions.retry" }]
                    }
                  ],
                  "successActions": [{ "reference": "$components.successActions.done" }],
                  "failureActions": [{ "reference": "$components.failureActions.retry" }]
                }
              ],
              "components": {
                "parameters": { "shared": { "name": "id", "in": "query", "value": "1" } },
                "successActions": { "done": { "name": "done", "type": "end" } },
                "failureActions": { "retry": { "name": "retry", "type": "retry" } }
              }
            }
            """)!;

        var document = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Empty(ctx.Diagnostic.Errors);
        Assert.NotNull(document.Workflows);
        var workflow = Assert.Single(document.Workflows);
        Assert.NotNull(workflow.Steps);
        var step = Assert.Single(workflow.Steps);
        Assert.NotNull(workflow.Parameters);
        Assert.NotNull(step.Parameters);
        Assert.NotNull(workflow.SuccessActions);
        Assert.NotNull(step.OnSuccess);
        Assert.NotNull(workflow.FailureActions);
        Assert.NotNull(step.OnFailure);
        Assert.IsType<ArazzoParameterReference>(Assert.Single(workflow.Parameters));
        Assert.IsType<ArazzoParameterReference>(Assert.Single(step.Parameters));
        Assert.IsType<ArazzoSuccessActionReference>(Assert.Single(workflow.SuccessActions));
        Assert.IsType<ArazzoSuccessActionReference>(Assert.Single(step.OnSuccess));
        Assert.IsType<ArazzoFailureActionReference>(Assert.Single(workflow.FailureActions));
        Assert.IsType<ArazzoFailureActionReference>(Assert.Single(step.OnFailure));
    }

    [Theory]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "missingWorkflow" }] }] }""", "references unknown workflowId 'missingWorkflow'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "$inputs.foo" }] }] }""", "workflowId value '$inputs.foo' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "$sourceDescriptions.missing.externalWorkflow" }] }] }""", "workflowId value '$sourceDescriptions.missing.externalWorkflow' references unknown sourceDescription 'missing'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api", "type": "openapi" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "$sourceDescriptions.source1.externalWorkflow" }] }] }""", "workflowId value '$sourceDescriptions.source1.externalWorkflow' references sourceDescription 'source1' whose type must be 'arazzo'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "workflowId": "$sourceDescriptions.source1.externalWorkflow" }] }] }""", "workflowId value '$sourceDescriptions.source1.externalWorkflow' references sourceDescription 'source1' whose type must be 'arazzo'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }], "successActions": [{ "name": "goto", "type": "goto", "stepId": "missingStep" }] }] }""", "references unknown stepId 'missingStep'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }], "successActions": [{ "name": "goto", "type": "goto", "workflowId": "$outputs.foo" }] }] }""", "workflowId value '$outputs.foo' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api", "type": "openapi" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1" }], "successActions": [{ "name": "goto", "type": "goto", "workflowId": "$sourceDescriptions.source1.externalWorkflow" }] }] }""", "workflowId value '$sourceDescriptions.source1.externalWorkflow' references sourceDescription 'source1' whose type must be 'arazzo'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "onFailure": [{ "name": "retry", "type": "retry", "workflowId": "$steps.step1" }] }] }] }""", "workflowId value '$steps.step1' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api", "type": "openapi" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "onFailure": [{ "name": "retry", "type": "retry", "workflowId": "$sourceDescriptions.source1.externalWorkflow" }] }] }] }""", "workflowId value '$sourceDescriptions.source1.externalWorkflow' references sourceDescription 'source1' whose type must be 'arazzo'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "parameters": [{ "reference": "$components.parameters.missing" }] }] }] }""", "reference '$components.parameters.missing' does not resolve")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationPath": "{$sourceDescriptions.missing.url}#/paths/~1users/get" }] }] }""", "references unknown sourceDescription 'missing'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationPath": "/users/{id}" }] }] }""", "must reference a sourceDescription URL runtime expression followed by a JSON Pointer to an operation path")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "steps": [{ "stepId": "step1", "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users/invalid" }] }] }""", "must reference a sourceDescription URL runtime expression followed by a JSON Pointer to an operation path")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "dependsOn": ["missingWorkflow"], "steps": [{ "stepId": "step1" }] }] }""", "dependsOn references unknown workflowId 'missingWorkflow'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "dependsOn": ["$sourceDescriptions.missing.externalWorkflow"], "steps": [{ "stepId": "step1" }] }] }""", "dependsOn value '$sourceDescriptions.missing.externalWorkflow' references unknown sourceDescription 'missing'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api", "type": "openapi" }], "workflows": [{ "workflowId": "wf", "dependsOn": ["$sourceDescriptions.source1.externalWorkflow"], "steps": [{ "stepId": "step1" }] }] }""", "dependsOn value '$sourceDescriptions.source1.externalWorkflow' references sourceDescription 'source1' whose type must be 'arazzo'")]
    [InlineData("""{ "arazzo": "1.0.1", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [{ "name": "source1", "url": "https://example.com/api" }], "workflows": [{ "workflowId": "wf", "dependsOn": ["$steps.step1"], "steps": [{ "stepId": "step1" }] }] }""", "dependsOn value '$steps.step1' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'")]
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
              "sourceDescriptions": [
                { "name": "source1", "url": "https://example.com/api" },
                { "name": "external", "url": "https://example.com/external.arazzo", "type": "arazzo" }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "dependsOn": ["child", "$sourceDescriptions.external.externalWorkflow"],
                  "steps": [
                    {
                      "stepId": "step1",
                      "operationPath": "{$sourceDescriptions.source1.url}#/paths/~1users/get",
                      "parameters": [{ "reference": "$components.parameters.shared" }]
                     },
                     {
                       "stepId": "step2",
                       "workflowId": "$sourceDescriptions.external.childWorkflow"
                     }
                  ],
                  "successActions": [{ "name": "goto", "type": "goto", "workflowId": "$sourceDescriptions.external.childWorkflow" }],
                  "failureActions": [{ "name": "retry", "type": "retry", "workflowId": "$sourceDescriptions.external.childWorkflow" }]
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

    [Theory]
    [InlineData("""{ "stepId": "step1", "operationPath": "{$sourceDescriptions.someArazzo.url}#/paths/~1pets/get" }""", "operationPath '{$sourceDescriptions.someArazzo.url}#/paths/~1pets/get' references sourceDescription 'someArazzo' with type 'arazzo'")]
    [InlineData("""{ "stepId": "step1", "operationId": "$sourceDescriptions.someArazzo.someOperation" }""", "operationId '$sourceDescriptions.someArazzo.someOperation' references sourceDescription 'someArazzo' with type 'arazzo'")]
    public void Parse_OperationReferenceToArazzoSourceDescription_AddsDiagnosticError(string stepJson, string expectedMessage)
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse($$"""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                },
                {
                  "name": "someArazzo",
                  "url": "https://example.com/other.arazzo",
                  "type": "arazzo"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [{{stepJson}}]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_UnqualifiedOperationIdWithOpenApiAndArazzoSourceDescriptions_DoesNotAddAmbiguityDiagnostic()
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
                },
                {
                  "name": "someArazzo",
                  "url": "https://example.com/other.arazzo",
                  "type": "arazzo"
                }
              ],
              "workflows": [
                {
                  "workflowId": "wf",
                  "steps": [
                    {
                      "stepId": "step1",
                      "operationId": "someOperation"
                    }
                  ]
                }
              ]
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.DoesNotContain(ctx.Diagnostic.Errors, e => e.Message.Contains("operationId 'someOperation' is ambiguous", StringComparison.Ordinal));
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
    public void ParseFragment_Arazzo1_1_ReturnsElement()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""{ "title": "T", "version": "1" }""")!;

        var info = ctx.ParseFragment<ArazzoInfo>(jsonNode, ArazzoSpecVersion.Arazzo1_1);

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