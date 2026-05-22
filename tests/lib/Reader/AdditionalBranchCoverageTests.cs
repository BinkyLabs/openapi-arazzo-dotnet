// Licensed under the MIT license.

using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class AdditionalBranchCoverageTests
{
    [Fact]
    public async Task ArazzoJsonReader_ReadAsync_MalformedJson_ReturnsDiagnosticError()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{not valid json"));
        var result = await reader.ReadAsync(stream, new Uri("https://example.com"), new ArazzoReaderSettings(), ct);
        Assert.Null(result.Document);
        Assert.NotEmpty(result.Diagnostic.Errors);
    }

    [Fact]
    public async Task ArazzoYamlReader_EmptyYaml_ReturnsDiagnosticOrThrows()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoYamlReader();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var ex = await Record.ExceptionAsync(() => reader.ReadAsync(stream, new Uri("https://example.com"), new ArazzoReaderSettings(), ct));
        // Either throws or returns a diagnostic — both exercise the < error path.
        Assert.NotNull(ex);
    }

    [Fact]
    public void ValidateRequiredFields_MissingInfo_AddsError()
    {
        var json = JsonNode.Parse("""{ "Arazzo": "1.0.0", "sourceDescriptions": [] }""")!;
        var diagnostic = new ArazzoDiagnostic();
        var context = new ParsingContext(diagnostic);
        // Parse will not throw — but Info missing should add an error.
        var doc = context.Parse(json, new Uri("https://example.com"));
        Assert.NotNull(doc);
        Assert.Contains(diagnostic.Errors, e => e.Message.Contains("Info", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddExtension_ExistingExtensionsDictionary_AppendsWithoutAllocatingNew()
    {
        var step = new ArazzoStep { StepId = "s1" };
        var ext = new JsonNodeExtension(JsonValue.Create("v")!);
        step.AddExtension("x-one", ext);
        var firstDict = step.Extensions;
        step.AddExtension("x-two", ext);
        // Same dictionary instance reused → exercises the non-null branch of `??=`.
        Assert.Same(firstDict, step.Extensions);
        Assert.Equal(2, step.Extensions!.Count);
    }

    [Fact]
    public void MapNode_GetScalarValue_NonScalarValue_Throws()
    {
        var json = JsonNode.Parse("""{ "key": { "nested": "x" } }""")!;
        var context = new ParsingContext(new ArazzoDiagnostic());
        var mapNode = new MapNode(context, json);
        var keyValue = new ValueNode(context, JsonValue.Create("key")!);
        Assert.Throws<ArazzoReaderException>(() => mapNode.GetScalarValue(keyValue));
    }

    private enum EnumWithoutDisplay
    {
        FieldA,
        FieldB
    }

    [Fact]
    public void TryGetEnumFromDisplayName_EnumWithoutDisplay_NotFound()
    {
        var result = StringExtensions.TryGetEnumFromDisplayName<EnumWithoutDisplay>("FieldA", out var value);
        Assert.False(result);
    }

    [Fact]
    public void ToFirstCharacterLowerCase_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, "".ToFirstCharacterLowerCase());
        Assert.Equal("aBC", "ABC".ToFirstCharacterLowerCase());
    }
}