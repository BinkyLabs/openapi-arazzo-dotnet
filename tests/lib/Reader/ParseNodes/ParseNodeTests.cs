// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader.ParseNodes;

public class ParseNodeTests
{
    private static ParsingContext Ctx() => new(new ArazzoDiagnostic());

    [Fact]
    public void Create_JsonArray_ReturnsListNode()
    {
        var ctx = Ctx();
        var node = ParseNode.Create(ctx, JsonNode.Parse("[]")!);
        Assert.IsType<ListNode>(node);
    }

    [Fact]
    public void Create_JsonObject_ReturnsMapNode()
    {
        var ctx = Ctx();
        var node = ParseNode.Create(ctx, JsonNode.Parse("{}")!);
        Assert.IsType<MapNode>(node);
    }

    [Fact]
    public void Create_JsonValue_ReturnsValueNode()
    {
        var ctx = Ctx();
        var node = ParseNode.Create(ctx, JsonValue.Create("hi")!);
        Assert.IsType<ValueNode>(node);
    }

    [Fact]
    public void CheckMapNode_OnNonMap_Throws()
    {
        var ctx = Ctx();
        var node = ParseNode.Create(ctx, JsonNode.Parse("[]")!);
        Assert.Throws<ArazzoReaderException>(() => node.CheckMapNode("foo"));
    }

    [Fact]
    public void CheckMapNode_OnMap_ReturnsMap()
    {
        var ctx = Ctx();
        var node = ParseNode.Create(ctx, JsonNode.Parse("{}")!);
        var map = node.CheckMapNode("foo");
        Assert.NotNull(map);
    }

    [Fact]
    public void ValueNode_GetScalarValue_ReturnsString()
    {
        var ctx = Ctx();
        var node = new ValueNode(ctx, JsonValue.Create("abc")!);
        Assert.Equal("abc", node.GetScalarValue());
    }

    [Fact]
    public void ValueNode_CreateAny_ReturnsNode()
    {
        var ctx = Ctx();
        var val = JsonValue.Create("abc")!;
        var node = new ValueNode(ctx, val);
        Assert.Same(val, node.CreateAny());
    }

    [Fact]
    public void ValueNode_Construct_FromNonValue_Throws()
    {
        var ctx = Ctx();
        Assert.Throws<ArazzoReaderException>(() => new ValueNode(ctx, JsonNode.Parse("{}")!));
    }

    [Fact]
    public void ListNode_CreateList_ProjectsObjects()
    {
        var ctx = Ctx();
        var list = new ListNode(ctx, JsonNode.Parse("""[{"a":1},{"b":2}]""")!.AsArray());

        var result = list.CreateList(m => m);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ListNode_CreateSimpleList_ProjectsValues()
    {
        var ctx = Ctx();
        var list = new ListNode(ctx, JsonNode.Parse("""["a","b"]""")!.AsArray());

        var result = list.CreateSimpleList(v => v.GetScalarValue());
        Assert.Equal(new[] { "a", "b" }, result);
    }

    [Fact]
    public void ListNode_CreateListOfAny_ReturnsItems()
    {
        var ctx = Ctx();
        var list = new ListNode(ctx, JsonNode.Parse("""[1,2,3]""")!.AsArray());
        var result = list.CreateListOfAny();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ListNode_CreateAny_ReturnsArray()
    {
        var ctx = Ctx();
        var arr = JsonNode.Parse("[1,2]")!.AsArray();
        var list = new ListNode(ctx, arr);
        Assert.Same(arr, list.CreateAny());
    }

    [Fact]
    public void ListNode_Enumerate_ReturnsParseNodes()
    {
        var ctx = Ctx();
        var list = new ListNode(ctx, JsonNode.Parse("[1,2]")!.AsArray());
        var items = list.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.IsType<ValueNode>(i));
    }

    [Fact]
    public void MapNode_Construct_FromNonObject_Throws()
    {
        var ctx = Ctx();
        Assert.Throws<ArazzoReaderException>(() => new MapNode(ctx, JsonNode.Parse("[]")!));
    }

    [Fact]
    public void MapNode_Indexer_ReturnsPropertyNodeOrNull()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":1}""")!);
        Assert.NotNull(map["a"]);
        Assert.Null(map["missing"]);
    }

    [Fact]
    public void MapNode_CreateMap_BuildsDictionary()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":{"x":1},"b":{"y":2}}""")!);
        var result = map.CreateMap(m => m.GetReferencePointer() ?? "no-ref");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void MapNode_CreateMap_NonObjectValueProducesDefault()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a": 1}""")!);
        var result = map.CreateMap<object?>(m => m);
        Assert.Null(result["a"]);
    }

    [Fact]
    public void MapNode_CreateSimpleMap_BuildsDictionary()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":"x","b":"y"}""")!);
        var result = map.CreateSimpleMap(v => v.GetScalarValue());
        Assert.Equal("x", result["a"]);
        Assert.Equal("y", result["b"]);
    }

    [Fact]
    public void MapNode_CreateSimpleMap_NonScalar_Throws()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":{}}""")!);
        Assert.Throws<ArazzoReaderException>(() => map.CreateSimpleMap(v => v.GetScalarValue()));
    }

    [Fact]
    public void MapNode_CreateArrayMap_BuildsDictionary()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":["x","y"],"b":["z"]}""")!);
        var result = map.CreateArrayMap(v => v.GetScalarValue());
        Assert.Equal(2, result["a"].Count);
        Assert.Single(result["b"]);
    }

    [Fact]
    public void MapNode_CreateArrayMap_NonArray_Throws()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":"x"}""")!);
        Assert.Throws<ArazzoReaderException>(() => map.CreateArrayMap(v => v.GetScalarValue()));
    }

    [Fact]
    public void MapNode_GetRaw_ReturnsJsonString()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":1}""")!);
        Assert.Contains("\"a\"", map.GetRaw());
    }

    [Fact]
    public void MapNode_GetReferencePointer_ReturnsRefOrNull()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"$ref":"#/a"}""")!);
        Assert.Equal("#/a", map.GetReferencePointer());

        var no = new MapNode(ctx, JsonNode.Parse("""{"x":1}""")!);
        Assert.Null(no.GetReferencePointer());
    }

    [Fact]
    public void MapNode_GetJsonSchemaIdentifier_ReturnsIdOrNull()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"$id":"foo"}""")!);
        Assert.Equal("foo", map.GetJsonSchemaIdentifier());

        var no = new MapNode(ctx, JsonNode.Parse("""{}""")!);
        Assert.Null(no.GetJsonSchemaIdentifier());
    }

    [Fact]
    public void MapNode_GetSummaryAndDescription_ReturnsValuesOrNull()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"summary":"s","description":"d"}""")!);
        Assert.Equal("s", map.GetSummaryValue());
        Assert.Equal("d", map.GetDescriptionValue());

        var no = new MapNode(ctx, JsonNode.Parse("""{}""")!);
        Assert.Null(no.GetSummaryValue());
        Assert.Null(no.GetDescriptionValue());
    }

    [Fact]
    public void MapNode_GetScalarValue_ByKey_ReturnsValue()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":"hi"}""")!);
        var keyNode = new ValueNode(ctx, JsonValue.Create("a")!);
        Assert.Equal("hi", map.GetScalarValue(keyNode));
    }

    [Fact]
    public void MapNode_GetScalarValue_NonScalarKey_Throws()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":{}}""")!);
        var keyNode = new ValueNode(ctx, JsonValue.Create("a")!);
        Assert.Throws<ArazzoReaderException>(() => map.GetScalarValue(keyNode));
    }

    [Fact]
    public void MapNode_CreateAny_ReturnsJsonObject()
    {
        var ctx = Ctx();
        var obj = JsonNode.Parse("""{"a":1}""")!.AsObject();
        var map = new MapNode(ctx, obj);
        Assert.Same(obj, map.CreateAny());
    }

    [Fact]
    public void MapNode_Enumerate_ReturnsProperties()
    {
        var ctx = Ctx();
        var map = new MapNode(ctx, JsonNode.Parse("""{"a":1,"b":2}""")!);
        var props = map.ToList();
        Assert.Equal(2, props.Count);
    }

    [Fact]
    public void PropertyNode_ParseField_CallsFixedFieldMap()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "foo", JsonValue.Create("bar")!);
        var holder = new Holder();
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>
        {
            ["foo"] = (h, v) => h.Value = v.GetScalarValue()
        };
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>();

        propNode.ParseField(holder, fixedFields, patternFields);

        Assert.Equal("bar", holder.Value);
    }

    [Fact]
    public void PropertyNode_ParseField_CallsPatternFieldMap()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "x-foo", JsonValue.Create("bar")!);
        var holder = new Holder();
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>();
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>
        {
            [k => k.StartsWith("x-")] = (h, k, v) => h.Value = $"{k}={v.GetScalarValue()}"
        };

        propNode.ParseField(holder, fixedFields, patternFields);

        Assert.Equal("x-foo=bar", holder.Value);
    }

    [Fact]
    public void PropertyNode_ParseField_UnknownField_AddsDiagnostic()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "unknown", JsonValue.Create("bar")!);
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>();
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>();

        propNode.ParseField(new Holder(), fixedFields, patternFields);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("unknown is not a valid property"));
    }

    [Fact]
    public void PropertyNode_ParseField_SchemaField_IsIgnored()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "$schema", JsonValue.Create("https://example.com/schema")!);
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>();
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>();

        propNode.ParseField(new Holder(), fixedFields, patternFields);

        Assert.Empty(ctx.Diagnostic.Errors);
    }

    [Fact]
    public void PropertyNode_ParseField_ArazzoReaderException_AddsDiagnostic()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "foo", JsonValue.Create("bar")!);
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>
        {
            ["foo"] = (_, _) => throw new ArazzoReaderException("reader-err")
        };
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>();

        propNode.ParseField(new Holder(), fixedFields, patternFields);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("reader-err"));
    }

    [Fact]
    public void PropertyNode_ParseField_OpenApiException_AddsDiagnostic()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "foo", JsonValue.Create("bar")!);
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>
        {
            ["foo"] = (_, _) => throw new OpenApiException("openapi-err")
        };
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>();

        propNode.ParseField(new Holder(), fixedFields, patternFields);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("openapi-err"));
    }

    [Fact]
    public void PropertyNode_ParseField_PatternThrowsArazzoReaderException_AddsDiagnostic()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "x-foo", JsonValue.Create("bar")!);
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>();
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>
        {
            [k => k.StartsWith("x-")] = (_, _, _) => throw new ArazzoReaderException("pattern-err")
        };

        propNode.ParseField(new Holder(), fixedFields, patternFields);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("pattern-err"));
    }

    [Fact]
    public void PropertyNode_ParseField_PatternThrowsOpenApiException_AddsDiagnostic()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "x-foo", JsonValue.Create("bar")!);
        var fixedFields = new Dictionary<string, Action<Holder, ParseNode>>();
        var patternFields = new Dictionary<Func<string, bool>, Action<Holder, string, ParseNode>>
        {
            [k => k.StartsWith("x-")] = (_, _, _) => throw new OpenApiException("pattern-openapi-err")
        };

        propNode.ParseField(new Holder(), fixedFields, patternFields);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("pattern-openapi-err"));
    }

    [Fact]
    public void PropertyNode_CreateAny_Throws()
    {
        var ctx = Ctx();
        var propNode = new PropertyNode(ctx, "foo", JsonValue.Create("bar")!);
        Assert.Throws<NotImplementedException>(() => propNode.CreateAny());
    }

    [Fact]
    public void RootNode_Find_ReturnsParseNodeOrNull()
    {
        var ctx = Ctx();
        var root = new RootNode(ctx, JsonNode.Parse("""{"a":{"b":"c"}}""")!);
        var node = root.Find(new JsonPointer("/a/b"));
        Assert.NotNull(node);
        Assert.IsType<ValueNode>(node);

        Assert.Null(root.Find(new JsonPointer("/missing")));
    }

    [Fact]
    public void RootNode_GetMap_ReturnsMapNode()
    {
        var ctx = Ctx();
        var root = new RootNode(ctx, JsonNode.Parse("""{"a":1}""")!);
        var map = root.GetMap();
        Assert.NotNull(map);
        Assert.NotNull(map["a"]);
    }

    [Fact]
    public void ParseNode_VirtualMethods_ThrowFromValueNode()
    {
        var ctx = Ctx();
        ParseNode v = new ValueNode(ctx, JsonValue.Create("x")!);
        Assert.Throws<ArazzoReaderException>(() => v.CreateList<int>(_ => 0));
        Assert.Throws<ArazzoReaderException>(() => v.CreateMap<int>(_ => 0));
        Assert.Throws<ArazzoReaderException>(() => v.CreateSimpleList<int>(_ => 0));
        Assert.Throws<ArazzoReaderException>(() => v.CreateSimpleMap<int>(_ => 0));
        Assert.Throws<ArazzoReaderException>(() => v.GetRaw());
        Assert.Throws<ArazzoReaderException>(() => v.CreateListOfAny());
        Assert.Throws<ArazzoReaderException>(() => v.CreateArrayMap<int>(_ => 0));
    }

    [Fact]
    public void ParseNode_VirtualMethods_GetScalarValue_OnMap_Throws()
    {
        var ctx = Ctx();
        ParseNode m = new MapNode(ctx, JsonNode.Parse("{}")!);
        Assert.Throws<ArazzoReaderException>(() => m.GetScalarValue());
    }

    private class Holder
    {
        public string? Value { get; set; }
    }
}